using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace XboxLiveTrace
{
    internal class ThrottledCallsRule : IRule
    {
        private Int32 m_throttledCallsCount = 0;
        private Int32 m_totalCalls = 0;

        public ThrottledCallsRule() : base(Constants.ThrottleCalls)
        {
        }

        public Int32 ThrottledCallCount
        {
            get { return m_throttledCallsCount; }
        }

        public Int32 TotalCalls
        {
            get { return m_totalCalls; }
        }

        public override void DeserializeJson(Newtonsoft.Json.Linq.JObject json)
        {
        }

        public override Newtonsoft.Json.Linq.JObject SerializeJson()
        {
            return new Newtonsoft.Json.Linq.JObject();
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            RuleResult result = InitializeResult(DisplayName, Description);

            m_totalCalls = items.Count();
            m_throttledCallsCount = items.Where(call => call.m_httpStatusCode == 429).Count();
            
            // We need to search over all of the calls to the endpoint
            for(int i = 0; i < m_totalCalls;)
            {
                var call = items.ElementAt(i);

                // If its not a throttled call, move to the next call
                if(call.m_httpStatusCode != 429)
                {
                    ++i;
                    continue;
                }

                // If it is throttled, start a list
                List<ServiceCallItem> throttledCallSet = new List<ServiceCallItem>();
                throttledCallSet.Add(items.ElementAt(i));
                var throttledCall = throttledCallSet.First();
                JObject response = JObject.Parse(throttledCall.m_rspBody);

                // If there are 2 or more throttled calls in a row the title is not properly handling the response
                while (++i < m_throttledCallsCount)
                {
                    var nextCall = items.ElementAt(i);

                    if (call.m_httpStatusCode != 429)
                    {
                        ++i;
                        break;
                    }
                    else
                    {
                        throttledCallSet.Add(call);
                    }
                }

                // One call is a warning as we expect that they back off after getting the 429 response
                if(throttledCallSet.Count == 1)
                {
                    result.AddViolation(ViolationLevel.Warning,
                                        String.Format("Throttled call detected on endpoint. Allowed: {0} over {1} seconds.",
                                                      response["maxRequests"], response["periodInSeconds"]),
                                                      throttledCall);
                }
                // More that one in a row means that the title didn't handle the 429 and we want them to fix that.
                else
                {
                    result.AddViolation(ViolationLevel.Error,
                                        String.Format("Sequence of throttled calls detected on endpoint. Allowed: {0} over {1} seconds.",
                                                      response["maxRequests"], response["periodInSeconds"]),
                                                      throttledCallSet);
                }

                throttledCallSet.Clear();
            }

             
            result.Results.Add("Total Calls", TotalCalls);
            result.Results.Add("Throttled Calls", ThrottledCallCount);
            result.Results.Add("Percentage", ((double)ThrottledCallCount) / TotalCalls);

            return result;
        }

        public static String DisplayName
        {
            get { return "Throttled Call Detection"; }
        }

        public static String Description
        {
            get { return "Reports the number of throttled calls per endpoint. Multiple throttled call indicates the title is not properly handling a 429 response. See results.log for call limits."; }
        }
    }
}
