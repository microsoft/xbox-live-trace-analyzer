// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace XboxLiveTrace
{
    internal class XR049Rule : IRule
    {
        private static String DisplayName = "XR049Rule";
        public XR049Rule() : base(DisplayName)
        {
        }

        public override void DeserializeJson(JObject json)
        {
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            bool richpresenceFound = false;
            var richPresenceJsonObj = new { activity = new Object() };
            foreach (var call in items)
            {
                var richPresenceResult = JsonConvert.DeserializeAnonymousType(call.m_reqBody, richPresenceJsonObj);
                if (richPresenceResult != null && richPresenceResult.activity != null)
                {
                    richpresenceFound = true;
                    break;
                }
            }

            RuleResult result = InitializeResult(DisplayName, "");
            if (!richpresenceFound)
            {
                result.Violations.Add(new Violation());
            }
            return result;
        }

        public override JObject SerializeJson()
        {
            return null;
        }
    }
}
