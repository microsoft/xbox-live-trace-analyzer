// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace XboxLiveTrace
{
    internal class RulesEngine
    {
        private Dictionary<String, LinkedList<IRule>> m_ruleDefines = new Dictionary<String, LinkedList<IRule>>();
        private Dictionary<String, LinkedList<IRule>> m_endpointRuleMap = new Dictionary<String, LinkedList<IRule>>();
        private Dictionary<String,ConcurrentBag<RuleResult>> m_results = new Dictionary<string, ConcurrentBag<RuleResult>>();
        private String m_version = String.Empty;
        private String m_ruleFile = String.Empty;

        private static String WildCardToRegular(String value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

        public String RuleFile
        {
            get { return m_ruleFile; }
        }

        public String Version
        {
            get { return m_version; }
        }

        private uint m_counter = 0;
        public static bool m_isInternal = false;

        public RulesEngine(bool isInternal)
        {
            m_isInternal = isInternal;
        }

        void MapRules(IEnumerable<String> endpoints)
        {
            foreach (string endpoint in endpoints)
            {
                var rules = new LinkedList<IRule>();

                foreach (var typeRules in m_ruleDefines)
                {
                    foreach (var rule in typeRules.Value)
                    {
                        var regex = new Regex(WildCardToRegular(rule.Endpoint));
                        if (regex.IsMatch(endpoint))
                        {
                            var newRule = rule.Clone();
                            newRule.Endpoint = endpoint;
                            rules.AddLast(newRule);

                            // We only apply the first rule that matches for each type
                            break;
                        }
                    }
                }

                m_endpointRuleMap.Add(endpoint, rules);
            }
        }

        public void RunRulesOnData(String console, Dictionary<String, LinkedList<ServiceCallItem>> history, Dictionary<String, ServiceCallStats> stats)
        {
            // Expand the wildcard (*) endpoint rules out to match the actual endpoints
            MapRules(history.Keys);

            if(!m_results.ContainsKey(console))
            {
                m_results.Add(console, new ConcurrentBag<RuleResult>());
            }

            // Now the rules can be run in parallel
            Parallel.ForEach(GetAllRules(), rule => 
            {
                if (history.ContainsKey(rule.Endpoint))
                {
                    m_results[console].Add(rule.Run(history[rule.Endpoint], stats[rule.Endpoint]));
                }
            });

            return;
        }

        public void AddRules(IEnumerable<IRule> rules)
        {
            foreach (var rule in rules)
            {
                AddRule(rule.Clone());
            }
        }

        public String AddRule(IRule rule)
        {
            ++m_counter;
            if(rule.Name == "")
            {
                rule.Name = rule.RuleID + "_" + m_counter;
            }

            // Sort the rules by rule id 
            if (!m_ruleDefines.ContainsKey(rule.RuleID))
            {
                m_ruleDefines.Add(rule.RuleID, new LinkedList<IRule>());
            }

            m_ruleDefines[rule.RuleID].AddLast(rule);
            return rule.Name;
        }

        public IRule GetRule(String ruleId)
        {
            foreach(var ruleType in m_ruleDefines.Values)
            {
                foreach (var rule in ruleType)
                {
                    if(ruleId == rule.Name)
                    {
                        return rule;
                    }
                }
            }
            return null;
        }

        public void RemoveRule(String ruleId)
        {
            foreach (var ruleType in m_ruleDefines.Values)
            {
                foreach (var rule in ruleType)
                {
                    if (ruleId == rule.Name)
                    {
                        ruleType.Remove(rule);
                        return;
                    }
                }
            }
        }

        public void ClearAllRules()
        {
            m_ruleDefines.Clear();
            m_endpointRuleMap.Clear();
        }

        public List<IRule> GetAllRules()
        {
            List<IRule> result = new List<IRule>();
            foreach (var ruleType in m_endpointRuleMap)
            {
                result.AddRange(ruleType.Value);
            }
            return result;
        }

        public List<RuleResult> GetResults(String console)
        {
            return m_results[console].ToList();
        }

        public void SerializeJson(String filePath)
        {
            JObject ruleJson = new JObject();

            ruleJson["Version"] = m_version;

            JArray rules = new JArray();

            foreach (var rule in GetAllRules())
            {
                var ruleObject = new JObject();

                ruleObject["Type"] = rule.RuleID;
                ruleObject["Name"] = rule.Name;
                ruleObject["Endpoint"] = rule.Endpoint;
                ruleObject["Properties"] = rule.SerializeJson();

                rules.Add(ruleObject);
            }

            ruleJson["Rules"] = rules;

            using (StreamWriter ruleFile = new StreamWriter(filePath))
            {
                using (JsonTextWriter writer = new JsonTextWriter(ruleFile))
                {
                    writer.Formatting = Formatting.Indented;
                    ruleJson.WriteTo(writer);
                }
            }
        }

        
    }
}
