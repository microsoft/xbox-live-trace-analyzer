// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace XboxLiveTrace
{    
    internal class BatchFrequencyRule : IRule 
    {
        public UInt32 m_BatchSetDetectionWindowsMs = Constants.BatchDetectionWindowPeriod;
        public Dictionary<string, string> m_MatchPatterns = new Dictionary<string, string>();
        public UInt32 m_totalBatchCallCount = 0;

        public BatchFrequencyRule() : base(Constants.BatchFrequency)
        {
        }

        public override JObject SerializeJson()
        {
            var json = new JObject();
            json["BatchSetDetectionWindowMs"] = m_BatchSetDetectionWindowsMs;
            return json;
        }

        public override void DeserializeJson(JObject json)
        {
            Utils.SafeAssign(ref m_BatchSetDetectionWindowsMs, json["BatchSetDetectionWindowMs"]);
            JArray patterns = null;
            Utils.SafeAssign(ref patterns, json["MatchPatterns"]);
            foreach(var pattern in patterns)
            {
                m_MatchPatterns.Add(pattern["BatchURI"].ToString(), pattern["XUIDListClass"].ToString());
            }
            
        }


        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName,Description);
            //check invalid log versions
            if (items.Count(item => item.m_logVersion == Constants.Version1509) > 0)
            {
                result.AddViolation(ViolationLevel.Warning, "Data version does not support this rule. You need an updated Xbox Live SDK to support this rule");
                return result;
            }

            StringBuilder description = new StringBuilder();

            // Traverse through each pattern set found in rule parameter
            foreach (var pattern in m_MatchPatterns)
            {
                Dictionary<ServiceCallItem, int> matchesFoundDict = new Dictionary<ServiceCallItem, int>();
                
                foreach (ServiceCallItem thisItem in items)
                {
                    Match match = Regex.Match(thisItem.m_uri, pattern.Key);
                    if (match.Success)
                    {
                        try
                        {
                            JObject requestBodyJSON = JObject.Parse(thisItem.m_reqBody);
                            var values = requestBodyJSON.SelectToken(pattern.Value) as JArray;
                            if (values != null)
                            {
                                matchesFoundDict.Add(thisItem, values.Count);
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }  // finished traversing calls made to endpoint

                m_totalBatchCallCount = (UInt32)matchesFoundDict.Count;

                // For all the matches found, report on batch sets which happened within a specific time window
                int numDictItems = matchesFoundDict.Count;
                if (numDictItems >= 2)
                {
                    int startWindowIdx = 0;
                    List<ServiceCallItem> callsWithinWindow = new List<ServiceCallItem>();
                    int totalXUIDsForWindow = 0; 
                    totalXUIDsForWindow += matchesFoundDict.Values.ElementAt(startWindowIdx);
                    callsWithinWindow.Add(matchesFoundDict.Keys.ElementAt(startWindowIdx));
                    for (int endWindowIdx = 1; endWindowIdx < matchesFoundDict.Count(); ++endWindowIdx)
                    {
                        UInt64 timeElapsed = (UInt64)Math.Abs((float)
                            (matchesFoundDict.Keys.ElementAt(endWindowIdx).m_reqTimeUTC - matchesFoundDict.Keys.ElementAt(startWindowIdx).m_reqTimeUTC) / TimeSpan.TicksPerMillisecond);
                        if (timeElapsed <= m_BatchSetDetectionWindowsMs)
                        {
                            callsWithinWindow.Add(matchesFoundDict.Keys.ElementAt(endWindowIdx));
                            totalXUIDsForWindow += matchesFoundDict.Values.ElementAt(endWindowIdx);
                        }
                        else //exceeded window
                        {
                            if (callsWithinWindow.Count >= 2)
                            {
                                result.AddViolation(ViolationLevel.Warning, $"A set of {callsWithinWindow.Count} Batch Calls was found within a time window of ({m_BatchSetDetectionWindowsMs}ms) to endpoint. Total XUID count ({totalXUIDsForWindow}). Consider combining into one call.", callsWithinWindow);
                            }

                            startWindowIdx = endWindowIdx; // shift window
                            //reset figures
                            totalXUIDsForWindow = 0;
                            callsWithinWindow.Clear();
                        }
                    }
                    // in case we exited the last for early because we never exceeded the time window, then call
                    // the following function once more to handle any dangling reports.
                    if (callsWithinWindow.Count >= 2)
                    {
                        result.AddViolation(ViolationLevel.Warning, $"A set of {callsWithinWindow.Count} Batch Calls was found within a time window of ({m_BatchSetDetectionWindowsMs}ms) to endpoint. Total XUID count ({totalXUIDsForWindow}). Consider combining into one call.", callsWithinWindow);
                    }
                }
            } // end of foreach pattern in patterns

             
            result.Results.Add("Total Batch Calls", m_totalBatchCallCount);
            result.Results.Add("Allowed Time Between Calls in ms", m_BatchSetDetectionWindowsMs);
            result.Results.Add("Times Exceeded", result.ViolationCount);
            result.Results.Add("Potential Reduced Call Count", m_totalBatchCallCount - result.ViolationCount);

            return result;
        }

        public static String DisplayName
        {
            get { return "Batch Frequency"; }
        }

        public static String Description
        {
            get { return "Detects when a series of batch API calls are made close together that can potentially be combined to reduce the overall number of calls to the endpoint."; }
        }
    }
}
