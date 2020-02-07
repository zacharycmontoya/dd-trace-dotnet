using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Abstractions;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Export;
using OTSpanData = OpenTelemetry.Trace.Export.SpanData;

namespace Datadog.Trace.OpenTelemetry
{
    public sealed class DatadogExporter : SpanExporter
    {
        private readonly IDatadogTracer _tracer;

        public DatadogExporter(Tracer tracer)
        {
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        }

        /// <summary>Exports batch of spans asynchronously.</summary>
        /// <param name="batch">Batch of spans to export.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of export.</returns>
        public override Task<ExportResult> ExportAsync(IEnumerable<OTSpanData> batch,
                                                       CancellationToken cancellationToken)
        {
            var spans = batch.Select(sd => (ISpanData)new SpanData
                                                      {
                                                          SpanId = ToUInt64(sd.Context.SpanId),
                                                          TraceId = ToUInt64(sd.Context.TraceId),
                                                          ParentId = ToUInt64(sd.ParentSpanId),
                                                          OperationName = sd.Name,
                                                          StartTime = sd.StartTimestamp,
                                                          Duration = sd.EndTimestamp - sd.StartTimestamp,
                                                          Error = sd.Status != Status.Ok
                                                      }).ToList();

            // enqueue this batch of spans as a "trace" because that's all we support for now
            _tracer.AgentWriter.WriteTrace(spans);

            return Task.FromResult(ExportResult.Success);
        }

        private static ulong ToUInt64(ActivityTraceId activityTraceId)
        {
            var traceIdBytes = new byte[16];
            activityTraceId.CopyTo(traceIdBytes);
            return BitConverter.ToUInt64(traceIdBytes, 8);
        }

        private static ulong ToUInt64(ActivitySpanId activitySpanId)
        {
            var spanIdBytes = new byte[8];
            activitySpanId.CopyTo(spanIdBytes);
            return BitConverter.ToUInt64(spanIdBytes, 0);
        }

        /// <summary>Shuts down exporter asynchronously.</summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public override Task ShutdownAsync(CancellationToken cancellationToken)
        {
            return _tracer.AgentWriter.FlushAndCloseAsync();
        }
    }
}
