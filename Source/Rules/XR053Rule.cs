using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace XboxLiveTrace
{
    internal class XR053Rule : IRule
    {
        private static String DisplayName = "XR053Rule";
        public XR053Rule() : base(DisplayName)
        {
        }

        public override void DeserializeJson(JObject json)
        {
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            var result = InitializeResult("");

            // Check events type
            if (items.Count() == 0)
                return result;

            var mpEvents = items.Where(e => e.m_eventName.Contains("MultiplayerRound"));
            var mpStartEvents = mpEvents.Where(e => e.m_eventName.Contains("Start"));
            var mpEndEvents = mpEvents.Where(e => e.m_eventName.Contains("End"));

            var debugData = mpEvents.Select(e => new { Name = e.m_eventName, ID = e.m_multiplayerCorrelationId });

            var matchEvents = mpEvents.GroupBy(e => e.m_multiplayerCorrelationId);

            // Edge case if a correlation id was reused
            var duplicates = matchEvents.Where(g => g.Count() > 2);

            var matchedTotalCount = matchEvents.Count(g => g.Count() == 2);
            var mismatchedEvents = matchEvents.Where(g => g.Count() == 1);

            var mismatchedStartEventCount = mismatchedEvents.Count(g => g.Any(c => c.m_eventName.Contains("Start")));
            var mismatchedEndEventCount = mismatchedEvents.Count(g => g.Any(c => c.m_eventName.Contains("End")));

            foreach(var set in duplicates)
            {
                var starts = set.Count(e => e.m_eventName.Contains("Start"));
                var ends = set.Count(e => e.m_eventName.Contains("End"));

                if(starts > ends)
                {
                    mismatchedStartEventCount += (starts - ends);
                }
                else if(starts < ends)
                {
                    mismatchedEndEventCount += (ends - starts);
                }
            }

            var mismatchedTotalCount = mismatchedEvents.Count();

            double startToEndRatio = (double)mpStartEvents.Count() / (double)mpEndEvents.Count();
            double unmatchedStartRatio = (double)mismatchedStartEventCount / (double)mpStartEvents.Count();
            double unmatchedEndRatio = (double)mismatchedEndEventCount / (double)mpEndEvents.Count();

            if (startToEndRatio < .9 || startToEndRatio > 1.1)
            {
                result.AddViolation(ViolationLevel.Error, $"Ratio of Start and End events is {startToEndRatio:0.0%}. Allowed range is 90-110%.");
            }
            if(unmatchedStartRatio > .1)
            {
                result.AddViolation(ViolationLevel.Error, $"{unmatchedStartRatio:0.0%} of MultiplayerRoundStart events were unmatched. Allowed amount is less than 10%.");
            }
            if (unmatchedEndRatio > .1)
            {
                result.AddViolation(ViolationLevel.Error, $"{unmatchedEndRatio:0.0%} of MultiplayerRoundEnd events were unmatched. Allowed amount is less than 10%.");
            }

            result.Results.Add("TotalStartEvents", mpStartEvents.Count());
            result.Results.Add("TotalEndEvents", mpEndEvents.Count());
            result.Results.Add("StartToEndRatio", startToEndRatio);
            result.Results.Add("UnmatchedStartEventCount", mismatchedStartEventCount);
            result.Results.Add("UnmatchedStartPercentage", unmatchedStartRatio);
            result.Results.Add("UnmatchedEndEventCount", mismatchedEndEventCount);
            result.Results.Add("UnmatchedEndPercentage", unmatchedEndRatio);

            return result;
        }

        public override JObject SerializeJson()
        {
            return null;
        }
    }
}
