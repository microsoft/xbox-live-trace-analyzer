// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace XboxLiveTrace
{
    internal class TitleIdRule : IRule
    {
        public List<string> m_SampleTitleIDs;
        public UInt32 m_SampleTitleIDCallsCount = 0;

        public TitleIdRule() : base(Constants.TitleID)
        {
            m_SampleTitleIDs = new List<string>();
        }

        public override void DeserializeJson(Newtonsoft.Json.Linq.JObject json)
        {
            JToken titleIds = json["TitleIDs"];
            if(titleIds != null && titleIds is JArray)
            {
                foreach(JToken titleIdToken in titleIds)
                {
                    string titleId = (string)titleIdToken;
                    m_SampleTitleIDs.Add(titleId);
                }
            }
        }

        public override Newtonsoft.Json.Linq.JObject SerializeJson()
        {
            JArray titleIdArray = new JArray();
            foreach(string titleId in m_SampleTitleIDs)
            {
                titleIdArray.Add(titleId);
            }

            JObject json = new JObject();
            json["TitleIDs"] = titleIdArray;

            return json;
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);

            Int32 totalCalls = items.Count();
            for (int i = 0; i < totalCalls; i++)
            {
                ServiceCallItem call = items.ElementAt(i);

                if (call.m_reqHeader.Contains("User-Agent: PlayFab Agent"))
                {
                    //PlayFab Host is {titleId}.playfabapi.com
                    string titleId = call.m_host.Split('.')[0];
                    if(m_SampleTitleIDs.Contains(titleId.ToUpper()))
                    {
                        result.AddViolation(ViolationLevel.Warning, "Usage of Sample Title ID " + titleId + " detected.");
                        m_SampleTitleIDCallsCount++;
                    }
                }
            }

            result.Results.Add("Total Calls to Sample Title ID", m_SampleTitleIDCallsCount);

            return result;
        }

        public static String DisplayName
        {
            get { return "Sample Title ID Detection"; }
        }

        public static String Description
        {
            get { return "Reports the usage of sample title IDs in a service call."; }
        }
    }
}
