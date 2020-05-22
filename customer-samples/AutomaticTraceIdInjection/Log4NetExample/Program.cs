using System.IO;
using Datadog.Trace;
using log4net;
using log4net.Config;

namespace Log4NetExample
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            // Initialize the Tracer at the start of the application, so that the
            // application's service, version, and env are added to all logs.
            // Automatic instrumentation will do this, but we're not using it in this example
            var tracer = Tracer.Instance;

            var logRepository = LogManager.GetRepository(typeof(Program).Assembly);
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            try
            {
                LogicalThreadContext.Properties["order-number"] = 1024;
                log.Info("Message before a trace.");
                using (var scope = tracer.StartActive("Log4NetExample - Main()"))
                {
                    log.Info("Message during a trace.");
                }
            }
            finally
            {
                LogicalThreadContext.Properties.Remove("order-number");
            }

            log.Info("Message after a trace.");
        }
    }
}
