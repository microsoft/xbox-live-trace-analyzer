// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace XboxLiveTrace
{
    internal class CallFrequencyRule : IRule
    {
        internal class RateLimits
        {
            public String m_description;
            public List<String> m_applicableSubpaths = new List<String>();

            public UInt32 m_sustainedTimePeriodSeconds = Constants.CallFrequencySustainedTimePeriod;
            public UInt32 m_sustainedCallLimit = Constants.CallFrequencySustainedAllowedCalls;
            public UInt32 m_burstTimePeriodSeconds = Constants.CallFrequencyBurstTimePeriod;
            public UInt32 m_burstCallLimit = Constants.CallFrequencyBurstAllowedCalls;

            public UInt64 m_avgTimeBetweenReqsMs = Constants.CallFrequencyAvgTimeBetweenReq;
            public UInt64 m_avgElapsedCallTimeMs = Constants.CallFrequencyAvgElapsedCallTime;
            public UInt64 m_maxElapsedCallTimeMs = Constants.CallFrequencyMaxElapsedCallTime;
        }

        // Some service endpoints have multiple sets of limits depending on subpath. For example, presence.xboxlive.com
        // has seperate limits for reading and writing presence.
        public List<RateLimits> m_rateLimits;
        public ServiceCallStats m_stats;
        public UInt32 m_endpointSustainedViolations = 0;
        public UInt32 m_endpointBurstViolations = 0;

        public CallFrequencyRule() : base(Constants.CallFrequency)
        {
            m_rateLimits = new List<RateLimits>();
        }


        override public RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);

            m_stats = stats;
            m_endpointSustainedViolations = 0;
            m_endpointBurstViolations = 0;

            // For set of limits, look through items to determine where excess calls occurred
            foreach (var limits in m_rateLimits)
            {
                // Filter the full list of service calls to those which apply to this set of limits
                List<ServiceCallItem> applicableCalls = items.Where(serviceCall => 
                {
                    foreach (var subpath in limits.m_applicableSubpaths)
                    {
                        var subpathRegex = new Regex("^" + Regex.Escape(subpath).Replace("\\?", ".").Replace("\\*", ".*") + "$");
                        if (subpathRegex.IsMatch(new Uri(serviceCall.m_uri).AbsolutePath))
                        {
                            return true;
                        }
                    }
                    return false;
                }).ToList();

                var sustainedExcessCallsPerWindow = Utils.GetExcessCallsForTimeWindow(applicableCalls, limits.m_sustainedTimePeriodSeconds * 1000, limits.m_sustainedCallLimit);
                var burstExcessCallsPerWindow = Utils.GetExcessCallsForTimeWindow(applicableCalls, limits.m_burstTimePeriodSeconds * 1000, limits.m_burstCallLimit);

                foreach (var excessCalls in sustainedExcessCallsPerWindow)
                {
                    if (excessCalls.Count >= limits.m_sustainedCallLimit * 10)
                    {
                        var desc = $"Exceeding rate limits for '{limits.m_description}' required for title certification( limit of {limits.m_sustainedCallLimit * 10} calls with {excessCalls.Count} calls in {limits.m_sustainedTimePeriodSeconds} seconds).  Failure to adhere to the specified limits may block a title from release, and in-production issues with released titles may result in service suspension up to and including title removal.";
                        result.AddViolation(ViolationLevel.Error, desc, excessCalls);
                    }
                    else
                    {
                        var desc = $"Call frequency above the sustained call limit for '{limits.m_description}' (limit of {limits.m_sustainedCallLimit} exceeded with {excessCalls.Count} calls in {limits.m_sustainedTimePeriodSeconds} seconds).";
                        result.AddViolation(ViolationLevel.Warning, desc, excessCalls);
                    }
                    m_endpointSustainedViolations++;
                }

                foreach (var excessCalls in burstExcessCallsPerWindow)
                {
                    var desc = $"Call frequency above the burst call limit for '{limits.m_description}' (limit of {limits.m_burstCallLimit} exceeded with {excessCalls.Count} calls in {limits.m_burstTimePeriodSeconds} seconds).";
                    result.AddViolation(ViolationLevel.Warning, desc, excessCalls);
                    m_endpointBurstViolations++;
                }

                // The following is information that would only be useful for internal purposes. 
                if (RulesEngine.m_isInternal)
                {
                    UInt64 avgTimeBetweenReqsMs = stats.m_avgTimeBetweenReqsMs;
                    UInt64 avgElapsedCallTimeMs = stats.m_avgElapsedCallTimeMs;
                    UInt64 maxElapsedCallTimeMs = stats.m_maxElapsedCallTimeMs;


                    if (avgTimeBetweenReqsMs > 0 && avgTimeBetweenReqsMs < limits.m_avgTimeBetweenReqsMs)
                    {
                        result.AddViolation(ViolationLevel.Warning, "Average time of " + avgTimeBetweenReqsMs + "ms between calls is too short");
                    }

                    if (avgElapsedCallTimeMs > 0 && avgElapsedCallTimeMs > limits.m_avgElapsedCallTimeMs)
                    {
                        result.AddViolation(ViolationLevel.Warning, "Calls are taking longer than expected to return " + avgElapsedCallTimeMs + "ms");
                    }

                    if (maxElapsedCallTimeMs > 0 && maxElapsedCallTimeMs > limits.m_maxElapsedCallTimeMs)
                    {
                        result.AddViolation(ViolationLevel.Warning, "The maximum call time for calls is greater than allowed " + maxElapsedCallTimeMs + "ms");
                    }
                }
            }

            result.Results.Add("Total Calls", m_stats == null ? 0 : m_stats.m_numCalls);
            result.Results.Add("Times Sustained Exceeded", m_endpointSustainedViolations);
            result.Results.Add("Times Burst Exceeded", m_endpointBurstViolations);

            return result;
        }

        public override Newtonsoft.Json.Linq.JObject SerializeJson()
        {
            var limitsArray = new JArray();

            foreach (var limits in m_rateLimits)
            {
                var limitsJson = new JObject();
                var subpathsArray = new JArray();
                foreach(var subpath in limits.m_applicableSubpaths)
                {
                    subpathsArray.Add(subpath);
                }

                limitsJson["Description"] = limits.m_description;
                limitsJson["Subpaths"] = subpathsArray;
                limitsJson["SustainedCallPeriod"] = limits.m_sustainedTimePeriodSeconds;
                limitsJson["SustainedCallLimit"] = limits.m_sustainedCallLimit;
                limitsJson["BurstCallPeriod"] = limits.m_burstTimePeriodSeconds;
                limitsJson["BurstCallLimit"] = limits.m_burstCallLimit;
                limitsJson["AvgTimeBetweenReqsMs"] = limits.m_avgTimeBetweenReqsMs;
                limitsJson["AvgElapsedCallTimeMs"] = limits.m_avgElapsedCallTimeMs;
                limitsJson["MaxElapsedCallTimeMs"] = limits.m_maxElapsedCallTimeMs;

                limitsArray.Add(limitsJson);
            }

            var json = new JObject();
            json["Limits"] = limitsArray;

            return json;
        }

        public override void DeserializeJson(JObject json)
        {
            var limitsArrayToken = json["Limits"];
            if (limitsArrayToken != null && limitsArrayToken is JArray)
            {
                foreach (var limitsToken in limitsArrayToken as JArray)
                {
                    var limits = new RateLimits { m_description = Endpoint };
                    var limitsJson = limitsToken as Newtonsoft.Json.Linq.JObject;

                    // Check which subpaths these rate limits apply to
                    var subpaths = limitsJson["Subpaths"] as JArray;
                    if (subpaths != null)
                    {
                        foreach (var subpathToken in subpaths)
                        {
                            limits.m_applicableSubpaths.Add(subpathToken.ToString());
                        }
                    }
                    else
                    {
                        // If subpaths field isn't specified, apply the limits to all calls to the endpoint
                        limits.m_applicableSubpaths.Add("*");
                    }

                    Utils.SafeAssign(ref limits.m_description, limitsJson["Description"]);
                    Utils.SafeAssign(ref limits.m_sustainedTimePeriodSeconds, limitsJson["SustainedCallPeriod"]);
                    Utils.SafeAssign(ref limits.m_sustainedCallLimit, limitsJson["SustainedCallLimit"]);
                    Utils.SafeAssign(ref limits.m_burstTimePeriodSeconds, limitsJson["BurstCallPeriod"]);
                    Utils.SafeAssign(ref limits.m_burstCallLimit, limitsJson["BurstCallLimit"]);
                    Utils.SafeAssign(ref limits.m_avgTimeBetweenReqsMs, limitsJson["AvgTimeBetweenReqsMs"]);
                    Utils.SafeAssign(ref limits.m_avgElapsedCallTimeMs, limitsJson["AvgElapsedCallTimeMs"]);
                    Utils.SafeAssign(ref limits.m_maxElapsedCallTimeMs, limitsJson["MaxElapsedCallTimeMs"]);

                    m_rateLimits.Add(limits);
                }
            }
        }

        public static String DisplayName
        {
            get { return "Call Frequency"; }
        }

        public static String Description
        {
            get { return "Analyzes the frequency of calls over the length of the capture and reports the number of times that the title exceeded the expected limits."; }
        }
    }
}
