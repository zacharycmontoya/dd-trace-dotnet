namespace Datadog.Trace.OpenTelemetry
{
    /// <summary>
    /// Contains options for <see cref="DatadogExporter"/>.
    /// </summary>
    public class DatadogExporterOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="Tracer"/> instance to use to send spans to Datadog.
        /// </summary>
        public Tracer Tracer { get; set; }
    }
}
