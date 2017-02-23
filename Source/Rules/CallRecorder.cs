// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace XboxLiveTrace
{
    internal class CallRecorder : IRule
    {
        private static String DisplayName = "CallRecorder";
        public CallRecorder() : base(DisplayName)
        {
        }

        public override void DeserializeJson(JObject json)
        {
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, "");
            result.Results.Add("Calls",items.AsEnumerable());
            return result;
        }

        public override JObject SerializeJson()
        {
            return null;
        }
    }
}
