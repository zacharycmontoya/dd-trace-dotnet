using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Collections;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.Agent
{
    internal class AgentWriter : IAgentWriter
    {
        private const int TraceBufferSize = 1000;

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<AgentWriter>();

        private readonly DoubleBuffer<IReadOnlyCollection<Span>> _traceBuffer;
        private readonly IStatsd _statsd;
        private readonly Task _flushTask;
        private readonly TaskCompletionSource<bool> _processExit = new TaskCompletionSource<bool>();

        private IApi _api;

        public AgentWriter(IApi api, IStatsd statsd)
        {
            _api = api;
            _statsd = statsd;
            _flushTask = Task.Run(FlushTracesTaskLoopAsync);

            _traceBuffer = new DoubleBuffer<IReadOnlyCollection<Span>>(() => new SingleBuffer<IReadOnlyCollection<Span>>(TraceBufferSize));
        }

        public void OverrideApi(IApi api)
        {
            _api = api;
        }

        public void WriteTrace(IReadOnlyCollection<Span> trace)
        {
            var success = _traceBuffer.Add(trace);

            if (!success)
            {
                Log.Debug("Trace buffer is full. Dropped a trace from the buffer.");
            }

            if (_statsd != null)
            {
                _statsd.AppendIncrementCount(TracerMetricNames.Queue.EnqueuedTraces);
                _statsd.AppendIncrementCount(TracerMetricNames.Queue.EnqueuedSpans, trace.Count);

                if (!success)
                {
                    _statsd.AppendIncrementCount(TracerMetricNames.Queue.DroppedTraces);
                    _statsd.AppendIncrementCount(TracerMetricNames.Queue.DroppedSpans, trace.Count);
                }

                _statsd.Send();
            }
        }

        public async Task FlushAndCloseAsync()
        {
            if (!_processExit.TrySetResult(true))
            {
                return;
            }

            await Task.WhenAny(_flushTask, Task.Delay(TimeSpan.FromSeconds(20)))
                      .ConfigureAwait(false);

            if (!_flushTask.IsCompleted)
            {
                Log.Warning("Could not flush all traces before process exit");
            }
        }

        private async Task FlushTracesAsync()
        {
            var traces = _traceBuffer.GetAll();

            if (_statsd != null)
            {
                var spanCount = traces.Sum(t => t.Count);

                _statsd.AppendIncrementCount(TracerMetricNames.Queue.DequeuedTraces, traces.Count);
                _statsd.AppendIncrementCount(TracerMetricNames.Queue.DequeuedSpans, spanCount);
                _statsd.AppendSetGauge(TracerMetricNames.Queue.MaxTraces, TraceBufferSize);
                _statsd.Send();
            }

            if (traces.Count > 0)
            {
                await _api.SendTracesAsync(traces).ConfigureAwait(false);
            }
        }

        private async Task FlushTracesTaskLoopAsync()
        {
            // use the same params array over and over,
            // instead of allocating a new one every time we flush
            var tasks = new Task[2];
            tasks[0] = _processExit.Task;

            while (true)
            {
                try
                {
                    tasks[1] = Task.Delay(TimeSpan.FromSeconds(1));

                    await Task.WhenAny(tasks)
                              .ConfigureAwait(false);

                    if (_processExit.Task.IsCompleted)
                    {
                        await FlushTracesAsync().ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        await FlushTracesAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An unhandled error occurred during the flushing task");
                }
            }
        }
    }
}
