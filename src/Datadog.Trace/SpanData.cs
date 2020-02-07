using System;
using System.Collections.Generic;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace
{
    public class SpanData : ISpanData
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

        /// <summary>Initializes a new instance of the <see cref="SpanData"></see> class.</summary>
        public SpanData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanData"></see> class
        /// by copying values from the specified <see cref="ISpanData"/>.
        /// </summary>
        public SpanData(ISpanData span)
        {
            TraceId = span.TraceId;
            SpanId = span.SpanId;
            ParentId = span.ParentId;
            StartTime = span.StartTime;
            Duration = span.Duration;
            OperationName = span.OperationName;
            ServiceName = span.ServiceName;
            ResourceName = span.ResourceName;
            Type = span.Type;
            Error = span.Error;
            Tags = span.Tags;
            Metrics = span.Metrics;
        }
    }
}
