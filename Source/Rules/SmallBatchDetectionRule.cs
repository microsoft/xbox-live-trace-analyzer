// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XboxLiveTrace
{
    using Newtonsoft.Json.Linq;
    internal class SmallBatchDetectionRule : IRule 
    {
        public UInt32 m_minBatchXUIDsPerBatchCall = 0;
        public Dictionary<string, string> m_MatchPatterns = new Dictionary<string, string>();
        public List<Tuple<int, int>> m_patternInstancesFound = new List<Tuple<int, int>>();

        public SmallBatchDetectionRule() : base(Constants.SmallBatchDetection)
        {
        }

        public override JObject SerializeJson()
        {
            var json = new JObject();
            json["MinBatchXUIDsPerBatchCall"] = m_minBatchXUIDsPerBatchCall;
            return json;
        }

        public override void DeserializeJson(JObject json)
        {
            Utils.SafeAssign(ref m_minBatchXUIDsPerBatchCall, json["MinBatchXUIDsPerBatchCall"]);
            JArray patterns = null;
            Utils.SafeAssign(ref patterns, json["MatchPatterns"]);
            foreach(var pattern in patterns)
            {
                m_MatchPatterns.Add(pattern["BatchURI"].ToString(), pattern["XUIDListClass"].ToString());
            }
            
        }

        public Tuple<int, int> PatternsFoundSumAsTuple()
        {
            int totalPatternInstancesFound = 0;
            int totalLowXUIDPatternsFound = 0;

            foreach (var tuple in m_patternInstancesFound)
            {
                totalPatternInstancesFound += tuple.Item1;
                totalLowXUIDPatternsFound += tuple.Item2;
            }

            return new Tuple<int, int>(totalPatternInstancesFound, totalLowXUIDPatternsFound);
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);
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

                int patternInstancesFound = 0;
                int lowXUIDInstancesFound = 0;
                
                // This first section reports on violations which are from batch calls made with not enough XUIDs in the request body
                foreach (ServiceCallItem thisItem in items)
                {
                    Match match = Regex.Match(thisItem.m_uri, pattern.Key);
                    if (match.Success)
                    {
                        if (!thisItem.m_reqHeader.Contains("SocialManager"))
                        {
                            patternInstancesFound++;
                            try
                            {
                                JObject requestBodyJSON = JObject.Parse(thisItem.m_reqBody);
                                var values = requestBodyJSON.SelectToken(pattern.Value) as JArray;
                                if (values != null)
                                {
                                    matchesFoundDict.Add(thisItem, values.Count);
                                    if (values.Count < m_minBatchXUIDsPerBatchCall)
                                    {
                                        lowXUIDInstancesFound++;
                                        description.Clear();
                                        description.AppendFormat("Batch call detected for endpoint for a small sized set of {0} XUIDs.", values.Count);
                                        result.AddViolation(ViolationLevel.Warning, description.ToString(), thisItem);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                // ignored
                            }
                        }
                    }
                }  // finished traversing calls made to endpoint

                m_patternInstancesFound.Add(new Tuple<int, int>(patternInstancesFound, lowXUIDInstancesFound));
            } // end of foreach pattern in patterns

            var finalStats = PatternsFoundSumAsTuple();

             
            result.Results.Add("Total Batch Calls", finalStats.Item1);
            result.Results.Add("Min. Users Allowed", m_minBatchXUIDsPerBatchCall);
            result.Results.Add("Calls Below Count", finalStats.Item2);
            result.Results.Add("% Below Count",(double)(finalStats.Item2) / finalStats.Item1);

            return result;
        }

        public static String DisplayName
        {
            get { return "Small-Batch Detection"; }
        }

        public static String Description
        {
            get { return "Detects uses of batch APIs with a small number of users."; }
        }
    }
}
