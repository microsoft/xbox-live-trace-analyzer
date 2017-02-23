// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Text;

namespace XboxLiveTrace
{
    internal class BurstDetectionRule : IRule
    {
        public UInt32 m_burstDetectionWindowMs;
        public UInt32 m_burstSizeToDetect = 0;

        public double m_avgCallsPerSecond = 0;
        public double m_callStdDeviationPerSecond = 0;

        public BurstDetectionRule() : base(Constants.BurstDetection)
        {
        }

        override public RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);

            StringBuilder description = new StringBuilder();

            m_avgCallsPerSecond = 1000.0 * 1.0 / stats.m_avgTimeBetweenReqsMs;
            m_callStdDeviationPerSecond = 1000.0 * 1.0 / Math.Sqrt(stats.m_varTimeBetweenReqsMs);

            const float factor = 2.0f;
            UInt32 burstSize = (UInt32)Math.Ceiling(m_avgCallsPerSecond) + (UInt32)Math.Ceiling(factor * m_callStdDeviationPerSecond);

            var allBurstsDetected = Utils.GetExcessCallsForTimeWindow(items, m_burstDetectionWindowMs, burstSize);
            // burst - is a list of calls (or just one call) that has exceeded the average requests per second rate
            foreach (var burst in allBurstsDetected)
            {
                if (burst.Count >= m_burstSizeToDetect)
                {
                    description.Clear();
                    description.AppendFormat("Burst increase of {0} calls to endpoint found.", burst.Count);

                    result.AddViolation(ViolationLevel.Warning, description.ToString(), burst);
                }
            }
            
            if (double.IsInfinity(m_avgCallsPerSecond))
            {
                 
                result.Results.Add("Avg. Calls Per Sec.", "N/A");
                result.Results.Add("Std. Deviation", "N/A");
                result.Results.Add("Burst Size", m_burstSizeToDetect);
                result.Results.Add("Burst Window", m_burstDetectionWindowMs);
                result.Results.Add("Total Bursts", result.ViolationCount);
            }
            else
            {
                 
                result.Results.Add("Avg. Calls Per Sec.", m_avgCallsPerSecond);
                result.Results.Add("Std. Deviation", m_callStdDeviationPerSecond);
                result.Results.Add("Burst Size", m_burstSizeToDetect);
                result.Results.Add("Burst Window", m_burstDetectionWindowMs);
                result.Results.Add("Total Bursts", result.ViolationCount);
            }

            return result;
        }

        public override Newtonsoft.Json.Linq.JObject SerializeJson()
        {
            var json = new Newtonsoft.Json.Linq.JObject();

            json["BurstDetectionWindowMs"] = m_burstDetectionWindowMs;
            json["BurstSizeToDetect"] = m_burstSizeToDetect;

            return json;
        }

        public override void DeserializeJson(Newtonsoft.Json.Linq.JObject json)
        {
            Utils.SafeAssign(ref m_burstDetectionWindowMs, json["BurstDetectionWindowMs"]);
            Utils.SafeAssign(ref m_burstSizeToDetect, json["BurstSizeToDetect"]);
        }

        public static String DisplayName
        {
            get { return "Burst Detection"; }
        }

        public static String Description
        {
            get { return "Analyzes the calls frequency and detects periods of increased calls for investigation."; }
        }
    }
}
