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
    internal class StatsReport : IReport
    {
        private bool m_json;
        public StatsReport(bool json)
        {
            m_json = json;
        }
        public void RunReport(String outputDirectory, IEnumerable<RuleResult> result, Dictionary<string, Tuple<string, string>> endpoints)
        {
            var stats = result.Where(r => r.RuleName == "StatsRecorder");

            JArray results = new JArray();

            foreach (var endpointStats in stats)
            {
                var stat = endpointStats.Results["Stats"] as ServiceCallStats;
                if (stat.m_numCalls == 0)
                {
                    continue;
                }

                JObject host = new JObject();
                host["Uri"] = endpointStats.Endpoint;

                if (endpoints.ContainsKey(endpointStats.Endpoint))
                {
                    host["Cpp"] = endpoints[endpointStats.Endpoint].Item1;
                    host["WinRT"] = endpoints[endpointStats.Endpoint].Item2;
                }

                host["Call Count"] = stat.m_numCalls;
                host["Average Time Between Calls"] = stat.m_avgTimeBetweenReqsMs;

                results.Add(host);
            }

            using (var output = new StreamWriter(Path.Combine(outputDirectory, m_json ? "stats.json" : "stats.js")))
            {
                if (!m_json)
                {
                    output.Write("stats = ");
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
