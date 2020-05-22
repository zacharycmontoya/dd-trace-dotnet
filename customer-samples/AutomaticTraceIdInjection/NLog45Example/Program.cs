using Datadog.Trace;
using NLog;

namespace NLog45Example
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            // Initialize the Tracer at the start of the application, so that the
            // application's service, version, and env are added to all logs.
            // Automatic instrumentation will do this, but we're not using it in this example
            var tracer = Tracer.Instance;

            using (MappedDiagnosticsLogicalContext.SetScoped("order-number", 1024))
            {
                Logger.Info("Message before a trace.");

                using (var scope = tracer.StartActive("NLog45Example - Main()"))
                {
                    Logger.Info("Message during a trace.");
                }

                Logger.Info("Message after a trace.");
            }
        }
    }
}
