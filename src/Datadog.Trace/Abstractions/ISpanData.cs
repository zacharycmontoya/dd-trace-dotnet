using System;
using System.Collections.Generic;

namespace Datadog.Trace.Abstractions
{
    internal interface ISpanData
    {
        ulong TraceId { get; }

        ulong SpanId { get; }

        ulong ParentId { get; }

        DateTimeOffset StartTime { get; }

        TimeSpan Duration { get; }

        string OperationName { get; }

        string ServiceName { get; }

        string ResourceName { get; }

        string Type { get; }

        bool Error { get; }

        IDictionary<string, string> Tags { get; }

        IDictionary<string, double> Metrics { get; }
    }
}
