using System;
using System.Collections.Generic;

namespace XboxLiveTrace
{
    public class ServiceCallStats
    {
        public UInt64 m_numCalls = 0;
        public double m_callEntropy = 0.0;
        public UInt64 m_lastReqTimeUTC = 0;
        public UInt64 m_avgTimeBetweenReqsMs = 0;
        public UInt64 m_varTimeBetweenReqsMs = 0;
        public UInt64 m_avgElapsedCallTimeMs = 0;
        public UInt64 m_varElapsedCallTimeMs = 0;
        public UInt64 m_maxElapsedCallTimeMs = 0;
        public UInt64 m_numSkippedCalls = 0;

        public enum CallType
        {
            CallType_Get = 0,
            CallType_NonGet,
            CallType_Count
        };

        public Dictionary<UInt64 /*RequestBodyHash*/, UInt32 /*count*/> m_reqBodyHashCountMap = new Dictionary<UInt64 /*RequestBodyHash*/, UInt32 /*count*/>();
    }
}
