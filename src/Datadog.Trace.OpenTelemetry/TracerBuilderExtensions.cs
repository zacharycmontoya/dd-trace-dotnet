using System;
using Datadog.Trace.OpenTelemetry;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Export;

namespace OpenTelemetry.Exporter.Datadog
{
    /// <summary>
    /// Extension methods to configure the <see cref="DatadogExporter"/>.
    /// </summary>
    public static class TracerBuilderExtensions
    {
        /// <summary>
        /// Registers a Datadog exporter.
        /// </summary>
        /// <param name="builder">Trace builder to use.</param>
        /// <param name="configure">Exporter configuration options.</param>
        /// <returns>The instance of <see cref="TracerBuilder"/> to chain the calls.</returns>
        public static TracerBuilder UseDatadog(this TracerBuilder builder, Action<DatadogExporterOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var options = new DatadogExporterOptions();
            configure(options);
            return builder.AddProcessorPipeline(b => b
                .SetExporter(new DatadogExporter(options))
                .SetExportingProcessor(e => new SimpleSpanProcessor(e)));
        }

        /// <summary>
        /// Registers Datadog exporter.
        /// </summary>
        /// <param name="builder">Trace builder to use.</param>
        /// <param name="configure">Exporter configuration options.</param>
        /// <param name="processorConfigure">Span processor configuration.</param>
        /// <returns>The instance of <see cref="TracerBuilder"/> to chain the calls.</returns>
        public static TracerBuilder UseDatadog(this TracerBuilder builder, Action<DatadogExporterOptions> configure, Action<SpanProcessorPipelineBuilder> processorConfigure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (processorConfigure == null)
            {
                throw new ArgumentNullException(nameof(processorConfigure));
            }

            var options = new DatadogExporterOptions();
            configure(options);
            return builder.AddProcessorPipeline(b =>
            {
                b.SetExporter(new DatadogExporter(options));
                processorConfigure.Invoke(b);
            });
        }
    }
}
