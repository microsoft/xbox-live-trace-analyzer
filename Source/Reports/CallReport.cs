// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XboxLiveTrace
{
    internal class CallReport : IReport
    {
        private bool m_json;
        public CallReport(bool json)
        {
            m_json = json;
        }
        public void RunReport(String outputDirectory, IEnumerable<RuleResult> result, Dictionary<string, Tuple<string, string, string>> endpoints)
        {
            var calls = result.Where(r => r.RuleName == "CallRecorder");

            JObject results = new JObject();
            JArray endpointArray = new JArray();

            double firstCall = double.MaxValue;
            double lastCall = double.MinValue;

            foreach (var callResult in calls)
            {
                var callList = callResult.Results["Calls"] as IEnumerable<ServiceCallItem>;

                if(callList.Count() == 0)
                {
                    continue;
                }

                JObject host = new JObject();
                host["Uri"] = callResult.Endpoint;

                if(endpoints.ContainsKey(callResult.Endpoint))
                {
                    host["Cpp"] = endpoints[callResult.Endpoint].Item1;
                    host["WinRT"] = endpoints[callResult.Endpoint].Item2;
                    host["C"] = endpoints[callResult.Endpoint].Item3;
                }

                JArray serviceCalls = new JArray();

                double hostFirstCall = callList.Min(c => c.m_reqTimeUTC) / (double)TimeSpan.TicksPerMillisecond;
                double hostLastCall = callList.Max(c => c.m_reqTimeUTC) / (double)TimeSpan.TicksPerMillisecond;

                if (hostFirstCall < firstCall) firstCall = hostFirstCall;
                if (hostLastCall > lastCall) lastCall = hostLastCall;

                foreach (var call in callList)
                {
                    JObject jsonCall = new JObject();

                    jsonCall["Id"] = call.m_id;
                    jsonCall["ReqTime"] = call.m_reqTimeUTC / (double)TimeSpan.TicksPerMillisecond;
                    jsonCall["Uri"] = call.m_uri;
                    if (call.m_xsapiMethods != null)
                    {
                        jsonCall["Cpp"] = call.m_xsapiMethods.Item1;
                        jsonCall["WinRT"] = call.m_xsapiMethods.Item2;
                        jsonCall["C"] = call.m_xsapiMethods.Item3;
                    }

                    jsonCall["Request Body"] = call.m_reqBody;

                    serviceCalls.Add(jsonCall);
                }

                host["Calls"] = serviceCalls;

                endpointArray.Add(host);
            }

            results["Start Time"] = firstCall;
            results["End Time"] = lastCall;
            results["Call List"] = endpointArray;

            using (var output = new StreamWriter(Path.Combine(outputDirectory, m_json ? "calls.json" : "calls.js")))
            {
                if (!m_json)
                {
                    output.Write("calls = ");
                }
                using (var jsonWriter = new JsonTextWriter(output))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    results.WriteTo(jsonWriter);
                }
            }
        }
    }
}
