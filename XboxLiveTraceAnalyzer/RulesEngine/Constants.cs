using System;

namespace XboxLiveTrace
{
    internal class Constants
    {
        public const String CallFrequency = "CallFrequency";
        public const String BurstDetection = "BurstDetection";
        public const String RepeatedCalls = "RepeatedCalls";
        public const String SmallBatchDetection = "SmallBatchDetection";
        public const String ThrottleCalls = "ThrottledCalls";
        public const String BatchFrequency = "BatchFrequency";
        public const String PollDetection = "PollingDetection";

        public const UInt32 CallFrequencySustainedTimePeriod = 300;
        public const UInt32 CallFrequencyBurstTimePeriod = 15;
        public const UInt32 CallFrequencySustainedAllowedCalls = 30;
        public const UInt32 CallFrequencyBurstAllowedCalls = 10;
        public const UInt64 CallFrequencyAvgTimeBetweenReq = 200;
        public const UInt64 CallFrequencyAvgElapsedCallTime = 3000;
        public const UInt64 CallFrequencyMaxElapsedCallTime = 500;
        public const UInt32 BatchDetectionWindowPeriod = 2000;
        public const String Version1509 = "v1509";
        public const String Version1510 = "v1510";
        public const float PollingDetectionSameDeltaThresholdMs = .01f;
        public const float PollingDetectionMaxFrequencyMs = 60 * 1000 * 60;
        public const int PollingDetectionMinSequenceSize = 5;
    }

    internal enum CSVValueIndex
    {
        Host = 0,
        Uri,
        XboxUserId,
        MultiplayerCorrelationId,
        RequestHeader,
        RequestBody,
        ResponseHeader,
        ResponseBody,
        HttpStatusCode,
        ElapsedCallTime,
        RequestTime,
        IsGet,
        Id,
        IsShoulderTap,
        ChangeNumber,
        SessionReferenceUriPath,
        IsInGameEvent,
        EventName, 
        PlayerSessionId,
        EventVersion,
        Dimensions,
        Measurements,
        BreadCrumb
    }
}
