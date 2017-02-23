// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XboxLiveTrace
{
    internal class RepeatedCallsRule : IRule
    {

        public UInt32 m_minAllowedRepeatIntervalMs;

        public Int32 m_totalCallsChecked = 0;
        public Int32 m_numberOfRepeats = 0;


        public RepeatedCallsRule() : base(Constants.RepeatedCalls)
        {
            m_minAllowedRepeatIntervalMs = 2000; 
        }


        override public RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);
            if (items.Count() == 0)
            {
                return result;
            }

            //check invalid log versions
            if (items.Count(item => item.m_logVersion == Constants.Version1509) > 0)
            {
                result.AddViolation(ViolationLevel.Warning, "Data version does not support this rule. You need an updated Xbox Live SDK to support this rule");
                return result;
            }

            StringBuilder description = new StringBuilder();

            List<ServiceCallItem> repeats = new List<ServiceCallItem>();

            m_totalCallsChecked = items.Count();

            foreach (ServiceCallItem thisItem in items.Where(item => item.m_isShoulderTap == false))
            {
                if (repeats.Contains(thisItem))
                {
                    continue;
                }

                var timeWindow = from item in items.Where(item => item.m_isShoulderTap == false)
                                 where (item.m_reqTimeUTC > thisItem.m_reqTimeUTC && ((item.m_reqTimeUTC - thisItem.m_reqTimeUTC) / TimeSpan.TicksPerMillisecond) < m_minAllowedRepeatIntervalMs)
                                 select item;

                List<ServiceCallItem> repeatedCalls = new List<ServiceCallItem>();

                repeatedCalls.Add(thisItem);

                foreach (var call in timeWindow)
                {
                    if (thisItem.m_reqBodyHash == call.m_reqBodyHash && thisItem.m_uri == call.m_uri)
                    {
                        repeatedCalls.Add(call);
                    }
                }

                if (repeatedCalls.Count > 1)
                {
                    description.Clear();
                    description.AppendFormat("Repeated call found {0} other times in calls to endpoint.", repeatedCalls.Count, thisItem.m_id);

                    result.AddViolation(ViolationLevel.Warning, description.ToString(), repeatedCalls);

                    repeats.AddRange(repeatedCalls);
                }
            }

            m_numberOfRepeats = repeats.Count;

             
            result.Results.Add("Total Calls", m_totalCallsChecked);
            result.Results.Add("Duplicates", m_numberOfRepeats);
            result.Results.Add("Percentage", ((double)m_numberOfRepeats) / m_totalCallsChecked);

            return result;
        }

        public override Newtonsoft.Json.Linq.JObject SerializeJson()
        {
            var json = new Newtonsoft.Json.Linq.JObject();

            json["MinAllowedRepeatIntervalMs"] = m_minAllowedRepeatIntervalMs;

            return json;
        }

        public override void DeserializeJson(Newtonsoft.Json.Linq.JObject json)
        {
            Utils.SafeAssign(ref m_minAllowedRepeatIntervalMs, json["MinAllowedRepeatIntervalMs"]);
        }

        public static String DisplayName
        {
            get { return "Repeated Calls"; }
        }

        public static String Description
        {
            get { return "Reports when identical calls are made within a small window of time."; }
        }
    }
}
