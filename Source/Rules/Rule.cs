using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XboxLiveTrace
{
    public enum ViolationLevel
    {
        Warning,
        Error,
        Info
    };

    // When a rule detects an issue, that issue is logged into a violation that will be stored in the RuleResult.
    public class Violation
    {
        public ViolationLevel m_level = ViolationLevel.Warning;
        public String m_endpoint = String.Empty;
        public String m_summary = String.Empty;
        public List<ServiceCallItem> m_violatingCalls = new List<ServiceCallItem>();

        public override String ToString()
        {
            if (m_violatingCalls.Count > 0)
            {
                StringBuilder idRange = new StringBuilder();
                Utils.PrintCallIdRange(idRange, m_violatingCalls, 10);
                return String.Format("[{0}][{1}] {2} ID(s): {3}", System.Enum.GetName(m_level.GetType(), m_level), m_endpoint, m_summary, idRange.ToString());
            }
            else
            {
                return String.Format("[{0}][{1}] {2}", System.Enum.GetName(m_level.GetType(), m_level), m_endpoint, m_summary);
            }
        }
    }

    public class RuleResult
    {
        // Name of the Rule the result came from
        public String RuleName { get; set; }
        // Endpoint the had the rule applied
        public String Endpoint { get; set; }
        // Description of the applied rule
        public String RuleDescription { get; set; }
        // List of the violations caught by this rule
        public List<Violation> Violations { get; }
        // Total Number of violations
        public Int32 ViolationCount { get { return Violations.Count; } }
        // Detailed information captured by the rule
        public Dictionary<String,Object> Results { get; }

        public RuleResult(String ruleName, String endpoint, String ruleDescription)
        {
            RuleName = ruleName;
            Endpoint = endpoint;
            RuleDescription = ruleDescription;
            Results = new Dictionary<String, Object>();
            Violations = new List<Violation>();
        }

        // Count the number of violations at the specified level
        public Int32 CountViolationLevel(ViolationLevel level)
        {
            return Violations.Count(v => v.m_level == level);
        }

        // Helper method to add a violation with a list of calls to the list.
        public void AddViolation(ViolationLevel level, String description, IEnumerable<ServiceCallItem> calls)
        {
            Violation v = new Violation();
            v.m_level = level;
            v.m_endpoint = Endpoint;
            v.m_summary = description;
            v.m_violatingCalls.AddRange(calls);
            Violations.Add(v);
        }

        // Helper method to add a violation with a single call
        public void AddViolation(ViolationLevel level, String description, ServiceCallItem call)
        {
            Violation v = new Violation();
            v.m_level = level;
            v.m_endpoint = Endpoint;
            v.m_summary = description;
            v.m_violatingCalls.Add(call);
            Violations.Add(v);
        }

        // Helper method to add a violation with no calls.
        public void AddViolation(ViolationLevel level, String description)
        {
            Violation v = new Violation();
            v.m_level = level;
            v.m_endpoint = Endpoint;
            v.m_summary = description;
            Violations.Add(v);
        }
    }

    public abstract class IRule
    {
        // Identifier for the type of rule
        public String RuleID { get; }

        // Identifier for the specific rule instance
        public String Name { get; set; }

        // Endpoint the rule will be analyzing
        public String Endpoint { get; set;  }

        // Base constructor that ensures a RuleID will exist.
        protected IRule(String ruleId)
        {
            RuleID = ruleId;
        }

        //  This method gets a JObject that has the data in the "Properties" object
        //  from the json description of the rule.
        //  {
        //      "Type": "{Class Name}",
        //      "Name": "{Rule Instance Name}",
        //      "Endpoint": "{Endpoint}", 
        //      "Properties":
        //      {  
        //          ...
        //      }
        //  }
        public abstract void DeserializeJson(Newtonsoft.Json.Linq.JObject json);
        // Fills in the JSON object properties section
        public abstract Newtonsoft.Json.Linq.JObject SerializeJson();
        // Parameters:
        //   - items: List of the ServiceCallItems that describe calls to this rule's endpoint
        //   - stats: Simple statistics that were computed while this set of calls was being processed.
        public abstract RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats);

        // If the rule is created with a wildcard endpoint ("*") then it will be duplicated by the Rule Engine
        // to make an individual instance for each endpoint.  If there are internal data structures that need
        // to be deep copied, override this method.
        public virtual IRule Clone()
        {
            var clone = this.MemberwiseClone() as IRule;
            return clone;
        }

        // Creates a RuleResult with this Rule's Name and Endpoint and a custom description.
        protected RuleResult InitializeResult(String description)
        {
            RuleResult result = new RuleResult(Name, Endpoint, description);
            return result;
        }

        // Creates a RuleResult with a cleaner display veersion of the Rule's name, Endpoint and a custom description. 
        protected RuleResult InitializeResult(String displayName, String description)
        {
            RuleResult result = new RuleResult(displayName, Endpoint, description);
            return result;
        }
    }
}
