using System;
using System.Collections.Generic;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace
{
    internal class SpanData : ISpanData
    {
        public ulong TraceId { get; set; }

        public ulong SpanId { get; set; }

        public ulong ParentId { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string OperationName { get; set; }

        public string ServiceName { get; set; }

        public string ResourceName { get; set; }

        public string Type { get; set; }

        public bool Error { get; set; }

        public IDictionary<string, string> Tags { get; set; }

        public IDictionary<string, double> Metrics { get; set; }
    }
}
