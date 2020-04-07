using System;

namespace Datadog.Trace
{
    internal interface ITraceContext
    {
        DateTimeOffset UtcNow { get; }

        SamplingPriority? SamplingPriority { get; set; }

        void AddSpan(Span span);

        void CloseSpan(Span span);

        void LockSamplingPriority();
    }
}
