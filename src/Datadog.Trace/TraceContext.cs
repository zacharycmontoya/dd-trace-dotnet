using System;
using System.Collections.Generic;
using System.Diagnostics;
using Datadog.Trace.Logging;
using Datadog.Trace.PlatformHelpers;

namespace Datadog.Trace
{
    internal class TraceContext : ITraceContext
    {
        private const int InitialSpanCapacity = 8;

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<TraceContext>();

        private readonly DateTimeOffset _utcStart = DateTimeOffset.UtcNow;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly object _spansLock = new object();

        private List<Span> _spans;
        private int _openSpans;
        private SamplingPriority? _samplingPriority;
        private bool _samplingPriorityLocked;

        public TraceContext(IDatadogTracer tracer)
        {
            Tracer = tracer;
        }

        public DateTimeOffset UtcNow => _utcStart.Add(_stopwatch.Elapsed);

        public IDatadogTracer Tracer { get; }

        /// <summary>
        /// Gets or sets sampling priority.
        /// Once the sampling priority is locked with <see cref="LockSamplingPriority"/>,
        /// further attempts to set this are ignored.
        /// </summary>
        public SamplingPriority? SamplingPriority
        {
            get => _samplingPriority;
            set
            {
                if (!_samplingPriorityLocked)
                {
                    _samplingPriority = value;
                }
            }
        }

        public void AddSpan(Span span)
        {
            lock (_spansLock)
            {
                if (_spans == null)
                {
                    _spans = new List<Span>(InitialSpanCapacity);
                }

                if (_spans.Count == 0)
                {
                    // first span added is the root span
                    DecorateRootSpan(span);

                    if (_samplingPriority == null)
                    {
                        if (span.Context.Parent is SpanContext context && context.SamplingPriority != null)
                        {
                            // this is a root span created from a propagated context that contains a sampling priority.
                            // lock sampling priority when a span is started from a propagated trace.
                            _samplingPriority = context.SamplingPriority;
                            LockSamplingPriority();
                        }
                        else
                        {
                            // this is a local root span (i.e. not propagated).
                            // determine an initial sampling priority for this trace, but don't lock it yet
                            _samplingPriority = Tracer.Sampler?.GetSamplingPriority(span);
                        }
                    }
                }

                _spans.Add(span);
                _openSpans++;
            }
        }

        public void CloseSpan(Span span)
        {
            // we only add spans to the list, never remove them,
            // so this is safe without locking.
            // first span is the root span.
            if (_spans?.Count > 0 && span == _spans[0])
            {
                // lock sampling priority and set metric when root span finishes
                LockSamplingPriority();

                if (_samplingPriority == null)
                {
                    Log.Warning("Cannot set span metric for sampling priority before it has been set.");
                }
                else
                {
                    span.SetMetric(Metrics.SamplingPriority, (int)_samplingPriority);
                }
            }

            IReadOnlyCollection<Span> spansToWrite = null;

            lock (_spansLock)
            {
                _openSpans--;

                if (_openSpans == 0)
                {
                    spansToWrite = _spans;
                    _spans = null;
                }
            }

            if (spansToWrite != null)
            {
                Tracer.Write(spansToWrite);
            }
        }

        public void LockSamplingPriority()
        {
            if (_samplingPriority == null)
            {
                Log.Warning("Cannot lock sampling priority before it has been set.");
            }
            else
            {
                _samplingPriorityLocked = true;
            }
        }

        private void DecorateRootSpan(Span span)
        {
            if (AzureAppServices.Metadata?.IsRelevant ?? false)
            {
                span.SetTag(Tags.AzureAppServicesSiteName, AzureAppServices.Metadata.SiteName);
                span.SetTag(Tags.AzureAppServicesResourceGroup, AzureAppServices.Metadata.ResourceGroup);
                span.SetTag(Tags.AzureAppServicesSubscriptionId, AzureAppServices.Metadata.SubscriptionId);
                span.SetTag(Tags.AzureAppServicesResourceId, AzureAppServices.Metadata.ResourceId);
            }
        }
    }
}
