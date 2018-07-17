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
    internal class PerEndpointJsonReport : IReport
    {
        private static String[] s_supportedRules = new String[] { BatchFrequencyRule.DisplayName,
                                                                  BurstDetectionRule.DisplayName,
                                                                  CallFrequencyRule.DisplayName,
                                                                  PollingDetectionRule.DisplayName,
                                                                  RepeatedCallsRule.DisplayName,
                                                                  SmallBatchDetectionRule.DisplayName,
                                                                  ThrottledCallsRule.DisplayName
                                                                };
        private bool m_json;
        public PerEndpointJsonReport(bool json)
        {
            m_json = json;
        }

        public void RunReport(String outputDirectory, IEnumerable<RuleResult> result, Dictionary<String, Tuple<String, String>> endpoints)
        {
            var endpointRules = result.GroupBy(r => r.Endpoint);

            JObject data = new JObject();
            JArray results = new JArray();

            foreach(var endpoint in endpoints)
            {
                Utils.AddNonNullItem(ref results, ReportEndpoint(endpointRules, endpoint));
            }

            data["Results"] = results;

            data["LTAVersion"] = TraceAnalyzer.CurrentVersion;

            GenerateMetaData(ref data, "Results");

            using (var output = new StreamWriter(Path.Combine(outputDirectory, m_json ? "report.json" : "report.js")))
            {
                if (!m_json)
                {
                    output.Write("data = ");
                }
                using (var jsonWriter = new JsonTextWriter(output))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    data.WriteTo(jsonWriter);
                }
            }
        }

        private static IEnumerable<RuleResult> GetEndpointResults(IEnumerable<IGrouping<String, RuleResult>> ruleGroups, String endpoint)
        {
            return ruleGroups.Where(g => g.Key == endpoint).SelectMany(g => g.AsEnumerable());
        }

        public JObject ReportEndpoint(IEnumerable<IGrouping<String, RuleResult>> ruleGroups, KeyValuePair<String, Tuple<String,String>> endpoint)
        {
            var endpointList = GetEndpointResults(ruleGroups, endpoint.Key);

            JObject endpointData = new JObject();

            if (endpointList.Count() == 0)
                return null;

            endpointData["UriService"] = endpoint.Key;
            endpointData["CppService"] = endpoint.Value.Item1;
            endpointData["WinRTService"] = endpoint.Value.Item2;

            JArray ruleResults = new JArray();

            foreach(var rule in endpointList)
            {
                Utils.AddNonNullItem(ref ruleResults, ReportRule(rule));
            }

            endpointData["Rules"] = ruleResults;

            GenerateMetaData(ref endpointData, "Rules");

            return endpointData;
        }

        public JObject ReportRule(RuleResult rule)
        {
            JObject jsonRule = new JObject();

            if (rule == null)
                return null;

            if (!s_supportedRules.Contains(rule.RuleName))
            {
                return null;
            }

            JObject jsonResult = new JObject();

            jsonRule["Name"] = rule.RuleName;
            jsonRule["Description"] = rule.RuleDescription;

            foreach (var data in rule.Results)
            {
                jsonResult[data.Key] = data.Value.ToString();
            }

            jsonRule["ResultData"] = jsonResult;

            foreach (ViolationLevel level in System.Enum.GetValues(typeof(ViolationLevel)))
            {
                jsonRule[System.Enum.GetName(typeof(ViolationLevel), level)] = rule.Violations.Count(v => v.m_level == level);
            }

            if (jsonRule[System.Enum.GetName(typeof(ViolationLevel), ViolationLevel.Error)].ToObject<Int32>() > 0)
            {
                jsonRule["Result"] = System.Enum.GetName(typeof(ViolationLevel), ViolationLevel.Error);
            }
            else if (jsonRule[System.Enum.GetName(typeof(ViolationLevel), ViolationLevel.Warning)].ToObject<Int32>() > 0)
            {
                jsonRule["Result"] = System.Enum.GetName(typeof(ViolationLevel), ViolationLevel.Warning);
            }
            else
            {
                jsonRule["Result"] = "Pass";
            }

            JArray jsonViolations = new JArray();

            foreach (var violation in rule.Violations)
            {
                JObject jsonViolation = new JObject();

                jsonViolation["Level"] = System.Enum.GetName(typeof(ViolationLevel), violation.m_level);
                jsonViolation["Summary"] = violation.m_summary;

                JArray calls = new JArray();

                foreach (var call in violation.m_violatingCalls)
                {
                    JObject jsonCall = new JObject();

                    jsonCall["Call Id"] = call.m_id;
                    jsonCall["UriMethod"] = call.m_uri;
                    if (call.m_xsapiMethods != null)
                    {
                        jsonCall["CppMethod"] = call.m_xsapiMethods.Item1;
                        jsonCall["WinRTMethod"] = call.m_xsapiMethods.Item2;
                    }

                    calls.Add(jsonCall);
                }

                jsonViolation["Calls"] = calls;

                jsonViolations.Add(jsonViolation);
            }

            jsonRule["Violations"] = jsonViolations;

            return jsonRule;
        }

        private void GenerateMetaData(ref JObject endpoint, String array)
        {
            ///JObject ruleResults = endpointResults["Rules"] as JObject;
            JArray endpointResults = endpoint[array] as JArray;
            Int32 warningCounts = 0;
            Int32 errorCounts = 0;

            foreach(JObject rule in endpointResults)
            {
                warningCounts += rule["Warning"].ToObject<Int32>();
                errorCounts += rule["Error"].ToObject<Int32>();
            }

            endpoint["Warning"] = warningCounts;
            endpoint["Error"] = errorCounts;

            if(errorCounts > 0)
            {
                endpoint["Result"] = "Error";
            }
            else if(warningCounts > 0)
            {
                endpoint["Result"] = "Warning";
            }
            else
            {
                endpoint["Result"] = "Pass";
            }
        }

    }
}
