using System.Collections.Generic;
using Datadog.Trace.Logging;
using Datadog.Trace.Logging.LogProviders;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Xunit;

namespace Datadog.Trace.Tests.Logging
{
    [Collection(nameof(Datadog.Trace.Tests.Logging))]
    [TestCaseOrderer("Datadog.Trace.TestHelpers.AlphabeticalOrderer", "Datadog.Trace.TestHelpers")]
    public class NLogLogProviderTests
    {
        private const string NLogExpectedStringFormat = "\"{0}\": \"{1}\"";

        private readonly ILogProvider _logProvider;
        private readonly ILog _logger;
        private readonly MemoryTarget _target;

        public NLogLogProviderTests()
        {
            var config = new LoggingConfiguration();
            var layout = new JsonLayout();
            layout.IncludeMdc = true;
            layout.Attributes.Add(new JsonAttribute("time", Layout.FromString("${longdate}")));
            layout.Attributes.Add(new JsonAttribute("level", Layout.FromString("${level:uppercase=true}")));
            layout.Attributes.Add(new JsonAttribute("message", Layout.FromString("${message}")));
            layout.Attributes.Add(new JsonAttribute("dd.service", Layout.FromString("${gdc:item=dd.service}")));
            layout.Attributes.Add(new JsonAttribute("dd.version", Layout.FromString("${gdc:item=dd.version}")));
            layout.Attributes.Add(new JsonAttribute("dd.env", Layout.FromString("${gdc:item=dd.env}")));

            _target = new MemoryTarget
            {
                Layout = layout
            };

            config.AddTarget("memory", _target);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Trace, _target));
            LogManager.Configuration = config;
            SimpleConfigurator.ConfigureForTargetLogging(_target, NLog.LogLevel.Trace);

            _logProvider = new NLogLogProvider();
            LogProvider.SetCurrentLogProvider(_logProvider);
            _logger = new LoggerExecutionWrapper(_logProvider.GetLogger("test"));
        }

        [Fact]
        public void LogsInjectionDisabled_DoesNotAddServiceIdentifiersAndCorrelationIdentifiers()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: false);
            LoggingProviderTestHelpers.LogEverywhere(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(filteredLogs, e => LogEventDoesNotContainServiceIdentifiers(e));
            Assert.All(filteredLogs, e => LogEventDoesNotContainCorrelationIdentifiers(e));
        }

        [Fact]
        public void LogsInjectionEnabled_AllLogs_AddsServiceIdentifiers()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogEverywhere(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(filteredLogs, e => LogEventContainsServiceIdentifiers(e, tracer.DefaultServiceName, tracer.Settings.ServiceVersion, tracer.Settings.Environment));
        }

        [Fact]
        public void LogsInjectionEnabled_InsideFirstLevelSpan_AddsCorrelationIdentifiers()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogInParentSpan(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(filteredLogs, e => LogEventContainsCorrelationIdentifiers(e, parentScope));
        }

        [Fact]
        public void LogsInjectionEnabled_InsideSecondLevelSpan_AddsCorrelationIdentifiers()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogInChildSpan(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(filteredLogs, e => LogEventContainsCorrelationIdentifiers(e, childScope));
        }

        [Fact]
        public void LogsInjectionEnabled_OutsideSpans_DoesNotAddCorrelationIdentifiers()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogOutsideSpans(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(filteredLogs, e => LogEventDoesNotContainCorrelationIdentifiers(e));
        }

        [Fact]
        public void LogsInjectionEnabled_CustomTraceServiceName_UsesTracerServiceName()
        {
            // Assert that the NLog log provider is correctly being used
            Assert.IsType<NLogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogInSpanWithCustomServiceName(tracer, _logger, _logProvider.OpenMappedContext, "custom-service", out var scope);

            // Filter the logs
            List<string> filteredLogs = new List<string>(_target.Logs);
            filteredLogs.RemoveAll(log => !log.Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(filteredLogs, e => LogEventContainsServiceIdentifiers(e, tracer.DefaultServiceName, tracer.Settings.ServiceVersion, tracer.Settings.Environment));
        }

        internal static void LogEventContainsCorrelationIdentifiers(string nLogString, Scope scope)
        {
            Assert.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.SpanIdKey, scope.Span.SpanId), nLogString);
            Assert.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.TraceIdKey, scope.Span.TraceId), nLogString);
        }

        internal static void LogEventContainsServiceIdentifiers(string nLogString, string service, string version, string env)
        {
            Assert.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.ServiceNameKey, service), nLogString);
            Assert.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.EnvKey, env), nLogString);
            Assert.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.ServiceVersionKey, version), nLogString);
        }

        internal static void LogEventDoesNotContainCorrelationIdentifiers(string nLogString)
        {
            Assert.True(
                nLogString.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.SpanIdKey, 0)) ||
                !nLogString.Contains($"\"{CorrelationIdentifier.SpanIdKey}\""));
            Assert.True(
                nLogString.Contains(string.Format(NLogExpectedStringFormat, CorrelationIdentifier.TraceIdKey, 0)) ||
                !nLogString.Contains($"\"{CorrelationIdentifier.TraceIdKey}\""));
        }

        internal static void LogEventDoesNotContainServiceIdentifiers(string nLogString)
        {
            Assert.True(!nLogString.Contains($"\"{CorrelationIdentifier.ServiceNameKey}\""));
            Assert.True(!nLogString.Contains($"\"{CorrelationIdentifier.ServiceVersionKey}\""));
            Assert.True(!nLogString.Contains($"\"{CorrelationIdentifier.EnvKey}\""));
        }
    }
}
