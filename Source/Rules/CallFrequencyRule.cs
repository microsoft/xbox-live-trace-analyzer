// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Text;

namespace XboxLiveTrace
{
    internal class CallFrequencyRule : IRule
    {
        public UInt32 m_sustainedTimePeriodSeconds;
        public UInt32 m_sustainedCallLimit;
        public UInt32 m_burstTimePeriodSeconds;
        public UInt32 m_burstCallLimit;

        public UInt64 m_avgTimeBetweenReqsMs;
        public UInt64 m_avgElapsedCallTimeMs;
        public UInt64 m_maxElapsedCallTimeMs;

        public ServiceCallStats m_stats;
        public UInt32 m_endpointSustainedViolations = 0;
        public UInt32 m_endpointBurstViolations = 0;


        public CallFrequencyRule() : base(Constants.CallFrequency)
        {
            m_sustainedTimePeriodSeconds = Constants.CallFrequencySustainedTimePeriod;
            m_sustainedCallLimit = Constants.CallFrequencySustainedAllowedCalls;
            m_burstTimePeriodSeconds = Constants.CallFrequencyBurstTimePeriod;
            m_burstCallLimit = Constants.CallFrequencyBurstAllowedCalls;
            m_avgTimeBetweenReqsMs = Constants.CallFrequencyAvgTimeBetweenReq;
            m_avgElapsedCallTimeMs = Constants.CallFrequencyAvgElapsedCallTime;
            m_maxElapsedCallTimeMs = Constants.CallFrequencyMaxElapsedCallTime;
        }


        override public RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);

            StringBuilder description = new StringBuilder();

            m_stats = stats;
            m_endpointSustainedViolations = 0;
            m_endpointBurstViolations = 0;

            // Look through items to determine where excess calls occurred
            var sustainedExcessCallsPerWindow = Utils.GetExcessCallsForTimeWindow(items, m_sustainedTimePeriodSeconds * 1000, (UInt32)(m_sustainedCallLimit * .9));
            var burstExcessCallsPerWindow = Utils.GetExcessCallsForTimeWindow(items, m_burstTimePeriodSeconds * 1000, (UInt32)(m_burstCallLimit * .9));

            foreach (var excessCalls in sustainedExcessCallsPerWindow)
            {
                description.Clear();
                if (excessCalls.Count < m_sustainedCallLimit)
                {
                    description.AppendFormat("Call frequency nearing the sustained call limit of {0} with {1} calls to endpoint.", m_sustainedCallLimit, excessCalls.Count);
                    result.AddViolation(ViolationLevel.Warning, description.ToString(), excessCalls);
                }
                else
                {
                    m_endpointSustainedViolations++;
                    description.AppendFormat("Call frequency above the sustained call limit of {0} with {1} calls to endpoint.", m_sustainedCallLimit, excessCalls.Count);
                    result.AddViolation(ViolationLevel.Error, description.ToString(), excessCalls);
                }
            }

            foreach (var excessCalls in burstExcessCallsPerWindow)
            {
                description.Clear();
                if (excessCalls.Count < m_burstCallLimit)
                {
                    description.AppendFormat("Call frequency nearing the burst call limit of {0} with {1} calls to endpoint.", m_burstCallLimit, excessCalls.Count);
                    result.AddViolation(ViolationLevel.Warning, description.ToString(), excessCalls);
                }
                else
                {
                    m_endpointBurstViolations++;
                    description.AppendFormat("Call frequency above the burst call limit of {0} with {1} calls to endpoint.", m_burstCallLimit, excessCalls.Count);
                    result.AddViolation(ViolationLevel.Error, description.ToString(), excessCalls);
                }
            }

            // The following is information that would only be useful for internal purposes. 
            if (RulesEngine.m_isInternal)
            {
                UInt64 avgTimeBetweenReqsMs = stats.m_avgTimeBetweenReqsMs;
                UInt64 avgElapsedCallTimeMs = stats.m_avgElapsedCallTimeMs;
                UInt64 maxElapsedCallTimeMs = stats.m_maxElapsedCallTimeMs;


                if (avgTimeBetweenReqsMs > 0 && avgTimeBetweenReqsMs < m_avgTimeBetweenReqsMs)
                {
                    result.AddViolation(ViolationLevel.Warning, "Average time of " + avgTimeBetweenReqsMs + "ms between calls is too short");
                }

                if (avgElapsedCallTimeMs > 0 && avgElapsedCallTimeMs > m_avgElapsedCallTimeMs)
                {
                    result.AddViolation(ViolationLevel.Warning, "Calls are taking longer than expected to return " + avgElapsedCallTimeMs + "ms");
                }

                if (maxElapsedCallTimeMs > 0 && maxElapsedCallTimeMs > m_maxElapsedCallTimeMs)
                {
                    result.AddViolation(ViolationLevel.Warning, "The maximum call time for calls is greater than allowed " + maxElapsedCallTimeMs + "ms");
                }
            }

             
            result.Results.Add("Total Calls", m_stats == null ? 0 : m_stats.m_numCalls);
            result.Results.Add("Sustained Call Period", m_sustainedTimePeriodSeconds.ToString() + "sec.");
            result.Results.Add("Sustained Call Limit", m_sustainedCallLimit);
            result.Results.Add("Times Sustained Exceeded", m_endpointSustainedViolations);
            result.Results.Add("Burst Call Period", m_burstTimePeriodSeconds.ToString() + "sec.");
            result.Results.Add("Burst Call Limit", m_burstCallLimit);
            result.Results.Add("Times Burst Exceeded", m_endpointBurstViolations);

            return result;
        }

        public override Newtonsoft.Json.Linq.JObject SerializeJson()
        {
            var json = new Newtonsoft.Json.Linq.JObject();

            json["SustainedCallPeriod"] = m_sustainedTimePeriodSeconds;
            json["SustainedCallLimit"] = m_sustainedCallLimit;
            json["BurstCallPeriod"] = m_burstTimePeriodSeconds;
            json["BurstCallLimit"] = m_burstCallLimit;
            json["AvgTimeBetweenReqsMs"] = m_avgTimeBetweenReqsMs;
            json["AvgElapsedCallTimeMs"] = m_avgElapsedCallTimeMs;
            json["MaxElapsedCallTimeMs"] = m_maxElapsedCallTimeMs;

            return json;
        }

        public override void DeserializeJson(Newtonsoft.Json.Linq.JObject json)
        {
            Utils.SafeAssign(ref m_sustainedTimePeriodSeconds, json["SustainedCallPeriod"]);
            Utils.SafeAssign(ref m_sustainedCallLimit, json["SustainedCallLimit"]);
            Utils.SafeAssign(ref m_burstTimePeriodSeconds, json["BurstCallPeriod"]);
            Utils.SafeAssign(ref m_burstCallLimit, json["BurstCallLimit"]);
            Utils.SafeAssign(ref m_avgTimeBetweenReqsMs, json["AvgTimeBetweenReqsMs"]);
            Utils.SafeAssign(ref m_avgElapsedCallTimeMs, json["AvgElapsedCallTimeMs"]);
            Utils.SafeAssign(ref m_maxElapsedCallTimeMs, json["MaxElapsedCallTimeMs"]);
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
