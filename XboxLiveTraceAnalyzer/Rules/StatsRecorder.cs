using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace XboxLiveTrace
{
    internal class StatsRecorder : IRule
    {
        private static String DisplayName = "StatsRecorder";
        public StatsRecorder() : base(DisplayName)
        {
        }

        public override void DeserializeJson(JObject json)
        {
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, "");
            result.Results.Add("Stats", stats);
            return result;
        }

        public override JObject SerializeJson()
        {
            return null;
        }
    }
}
