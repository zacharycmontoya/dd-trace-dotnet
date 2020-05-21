using System;
using System.Collections.Generic;
using Datadog.Trace.Logging;
using Datadog.Trace.Logging.LogProviders;
using Serilog;
using Serilog.Events;
using Xunit;

namespace Datadog.Trace.Tests.Logging
{
    [Collection(nameof(Datadog.Trace.Tests.Logging))]
    public class SerilogLogProviderTests
    {
        private readonly ILogProvider _logProvider;
        private readonly ILog _logger;
        private readonly List<LogEvent> _logEvents;

        public SerilogLogProviderTests()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Observers(obs => obs.Subscribe(logEvent => _logEvents.Add(logEvent)))
                .CreateLogger();
            _logEvents = new List<LogEvent>();

            _logProvider = new SerilogLogProvider();
            LogProvider.SetCurrentLogProvider(_logProvider);
            _logger = new LoggerExecutionWrapper(_logProvider.GetLogger("Test"));
        }

        [Fact]
        public void LogsInjectionDisabled_DoesNotAddServiceIdentifiersAndCorrelationIdentifiers()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: false);
            LoggingProviderTestHelpers.LogEverywhere(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(_logEvents, e => LogEventDoesNotContainServiceIdentifiers(e));
            Assert.All(_logEvents, e => LogEventDoesNotContainCorrelationIdentifiers(e));
        }

        [Fact]
        public void LogsInjectionEnabled_AllLogs_AddsServiceIdentifiers()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogEverywhere(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(_logEvents, e => LogEventContainsServiceIdentifiers(e, tracer.DefaultServiceName, tracer.Settings.ServiceVersion, tracer.Settings.Environment));
        }

        [Fact]
        public void LogsInjectionEnabled_InsideFirstLevelSpan_AddsCorrelationIdentifiers()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogInParentSpan(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(_logEvents, e => LogEventContainsCorrelationIdentifiers(e, parentScope));
        }

        [Fact]
        public void LogsInjectionEnabled_InsideSecondLevelSpan_AddsCorrelationIdentifiers()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogInChildSpan(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(_logEvents, e => LogEventContainsCorrelationIdentifiers(e, childScope));
        }

        [Fact]
        public void LogsInjectionEnabled_OutsideSpans_DoesNotAddCorrelationIdentifiers()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogOutsideSpans(tracer, _logger, _logProvider.OpenMappedContext, out var parentScope, out var childScope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(_logEvents, e => LogEventDoesNotContainCorrelationIdentifiers(e));
        }

        [Fact]
        public void LogsInjectionEnabled_CustomTraceServiceName_UsesTracerServiceName()
        {
            // Assert that the Serilog log provider is correctly being used
            Assert.IsType<SerilogLogProvider>(LogProvider.CurrentLogProvider);

            // Instantiate a tracer for this test with default settings and set LogsInjectionEnabled to TRUE
            var tracer = LoggingProviderTestHelpers.InitializeTracer(enableLogsInjection: true);
            LoggingProviderTestHelpers.LogInSpanWithCustomServiceName(tracer, _logger, _logProvider.OpenMappedContext, "custom-service", out var scope);

            // Filter the logs
            _logEvents.RemoveAll(log => !log.MessageTemplate.ToString().Contains(LoggingProviderTestHelpers.LogPrefix));
            Assert.All(_logEvents, e => LogEventContainsServiceIdentifiers(e, tracer.DefaultServiceName, tracer.Settings.ServiceVersion, tracer.Settings.Environment));
        }

        internal static void LogEventContainsServiceIdentifiers(Serilog.Events.LogEvent logEvent, string service, string version, string env)
        {
            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.ServiceNameKey));
            Assert.Equal(service, logEvent.Properties[CorrelationIdentifier.ServiceNameKey].ToString().Trim(new[] { '\"' }), ignoreCase: true);

            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.ServiceVersionKey));
            Assert.Equal(version, logEvent.Properties[CorrelationIdentifier.ServiceVersionKey].ToString().Trim(new[] { '\"' }), ignoreCase: true);

            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.EnvKey));
            Assert.Equal(env, logEvent.Properties[CorrelationIdentifier.EnvKey].ToString().Trim(new[] { '\"' }), ignoreCase: true);
        }

        internal static void LogEventContainsCorrelationIdentifiers(Serilog.Events.LogEvent logEvent, Scope scope)
        {
            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
            Assert.Equal(scope.Span.TraceId, ulong.Parse(logEvent.Properties[CorrelationIdentifier.TraceIdKey].ToString().Trim(new[] { '\"' })));

            Assert.True(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.Equal(scope.Span.SpanId, ulong.Parse(logEvent.Properties[CorrelationIdentifier.SpanIdKey].ToString().Trim(new[] { '\"' })));
        }

        internal static void LogEventDoesNotContainCorrelationIdentifiers(Serilog.Events.LogEvent logEvent)
        {
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.SpanIdKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.TraceIdKey));
        }

        internal static void LogEventDoesNotContainServiceIdentifiers(Serilog.Events.LogEvent logEvent)
        {
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.ServiceNameKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.ServiceVersionKey));
            Assert.False(logEvent.Properties.ContainsKey(CorrelationIdentifier.EnvKey));
        }
    }
}
