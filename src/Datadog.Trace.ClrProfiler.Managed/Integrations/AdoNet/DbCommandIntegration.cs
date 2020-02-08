using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ClrProfiler.Emit;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.Integrations.AdoNet
{
    /// <summary>
    /// Instrumentation wrappers for <see cref="DbCommand"/>.
    /// </summary>
    public static class DbCommandIntegration
    {
        // TODO: support both "DbCommand" (new name) and
        // "AdoNet" (backwards compatibility) when reading configuration settings
        private const string IntegrationName = "AdoNet";
        private const string Major4 = "4";

        private const string DbCommandTypeName = "System.Data.Common.DbCommand";
        private const string DbDataReaderTypeName = "System.Data.Common.DbDataReader";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(DbCommandIntegration));

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReader()"/>.
        /// </summary>
        /// <param name="command">The object referenced "this" in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { "System.Data", "System.Data.Common" },
            TargetType = "System.Data.Common.DbCommand",
            TargetMethod = "ExecuteReader",
            TargetSignatureTypes = new[] { "System.Data.Common.DbDataReader" },
            TargetMinimumVersion = "4.0.0",
            TargetMaximumVersion = "4.65535.65535")]
        public static object ExecuteReader(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = "ExecuteReader";
            Type instanceType = command.GetType();
            Type instrumentedType = command.GetInstrumentedType("System.Data.Common.DbCommand");
            Func<object, object> instrumentedMethod;

            // Use the MethodBuilder to construct a delegate to the original method call
            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<object, object>>
                        .Start(moduleVersionPtr, mdToken, opCode, methodName)
                        .WithConcreteType(instrumentedType)
                        .WithParameters()
                        .WithNamespaceAndNameFilters("System.Data.Common.DbDataReader") // Needed for the fallback logic if target method name is overloaded
                        .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    opCode: opCode,
                    mdToken: mdToken,
                    moduleVersionPointer: moduleVersionPtr,
                    methodName: methodName,
                    instanceType: instanceType?.AssemblyQualifiedName,
                    instrumentedType: "System.Data.Common.DbCommand");
                throw;
            }

            // Open a scope, decorate the span, and call the original method
            var dbCommand = command as IDbCommand;
            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(command);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReader(CommandBehavior)"/>.
        /// </summary>
        /// <param name="command">The object referenced "this" in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { "System.Data", "System.Data.Common" },
            TargetType = "System.Data.Common.DbCommand",
            TargetMethod = "ExecuteReader",
            TargetSignatureTypes = new[] { "System.Data.Common.DbDataReader", "System.Data.CommandBehavior" },
            TargetMinimumVersion = "4.0.0",
            TargetMaximumVersion = "4.65535.65535")]
        public static object ExecuteReaderWithBehavior(
            object command,
            int behavior,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = "ExecuteReader";
            Type instanceType = command.GetType();
            Type instrumentedType = command.GetInstrumentedType("System.Data.Common.DbCommand");
            var commandBehavior = (CommandBehavior)behavior;
            Func<object, CommandBehavior, object> instrumentedMethod;

            // Use the MethodBuilder to construct a delegate to the original method call
            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<object, CommandBehavior, object>>
                        .Start(moduleVersionPtr, mdToken, opCode, methodName)
                        .WithConcreteType(instrumentedType)
                        .WithParameters(commandBehavior)
                        .WithNamespaceAndNameFilters("System.Data.Common.DbDataReader", "System.Data.CommandBehavior") // Needed for the fallback logic if target method name is overloaded
                        .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    opCode: opCode,
                    mdToken: mdToken,
                    moduleVersionPointer: moduleVersionPtr,
                    methodName: methodName,
                    instanceType: instanceType?.AssemblyQualifiedName,
                    instrumentedType: "System.Data.Common.DbCommand");
                throw;
            }

            // Open a scope, decorate the span, and call the original method
            var dbCommand = command as IDbCommand;
            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(command, commandBehavior);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteReaderAsync()"/>.
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="behavior">The <see cref="CommandBehavior"/> value used in the original method call.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon },
            TargetType = DbCommandTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Data.Common.DbDataReader>", AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteReaderAsync(
            object command,
            int behavior,
            object cancellationTokenSource,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            var tokenSource = cancellationTokenSource as CancellationTokenSource;
            var cancellationToken = tokenSource?.Token ?? CancellationToken.None;

            return ExecuteReaderAsyncInternal(
                command as DbCommand,
                (CommandBehavior)behavior,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr);
        }

        private static async Task<DbDataReader> ExecuteReaderAsyncInternal(
            DbCommand command,
            CommandBehavior commandBehavior,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, CommandBehavior, CancellationToken, Task<DbDataReader>> instrumentedMethod;
            var instrumentedType = command.GetInstrumentedType(DbCommandTypeName);

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CommandBehavior, CancellationToken, Task<DbDataReader>>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteReaderAsync)
                       .WithConcreteType(instrumentedType)
                       .WithParameters(commandBehavior, cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, AdoNetConstants.TypeNames.CommandBehavior, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: DbCommandTypeName,
                    methodName: nameof(ExecuteReaderAsync),
                    instanceType: command.GetType().AssemblyQualifiedName);
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command, IntegrationName))
            {
                try
                {
                    return await instrumentedMethod(command, commandBehavior, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteNonQuery"/>.
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { "System.Data", "System.Data.Common" },
            TargetType = "System.Data.Common.DbCommand",
            TargetMethod = "ExecuteNonQuery",
            TargetSignatureTypes = new[] { "System.Int32" },
            TargetMinimumVersion = "4.0.0",
            TargetMaximumVersion = "4.65535.65535")]
        public static int ExecuteNonQuery(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = "ExecuteNonQuery";
            Type instanceType = command.GetType();
            Type instrumentedType = command.GetInstrumentedType("System.Data.Common.DbCommand");
            Func<object, int> instrumentedMethod;

            // Use the MethodBuilder to construct a delegate to the original method call
            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<object, int>>
                        .Start(moduleVersionPtr, mdToken, opCode, methodName)
                        .WithConcreteType(instrumentedType)
                        .WithParameters()
                        .WithNamespaceAndNameFilters("System.Int32") // Needed for the fallback logic if target method name is overloaded
                        .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    opCode: opCode,
                    mdToken: mdToken,
                    moduleVersionPointer: moduleVersionPtr,
                    methodName: methodName,
                    instanceType: instanceType?.AssemblyQualifiedName,
                    instrumentedType: "System.Data.Common.DbCommand");
                throw;
            }

            // Open a scope, decorate the span, and call the original method
            var dbCommand = command as IDbCommand;
            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(command);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteNonQueryAsync(CancellationToken)"/>
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon },
            TargetType = DbCommandTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Int32>", ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteNonQueryAsync(
            object command,
            object cancellationTokenSource,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            var tokenSource = cancellationTokenSource as CancellationTokenSource;
            var cancellationToken = tokenSource?.Token ?? CancellationToken.None;

            return ExecuteNonQueryAsyncInternal(
                command as DbCommand,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr);
        }

        private static async Task<int> ExecuteNonQueryAsyncInternal(
            DbCommand command,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, CancellationToken, Task<int>> instrumentedMethod;
            var instrumentedType = command.GetInstrumentedType(DbCommandTypeName);

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CancellationToken, Task<int>>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteNonQueryAsync)
                       .WithConcreteType(instrumentedType)
                       .WithParameters(cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving {DbCommandTypeName}.{AdoNetConstants.MethodNames.ExecuteNonQueryAsync}(...)");
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command, IntegrationName))
            {
                try
                {
                    return await instrumentedMethod(command, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteScalar"/>
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { "System.Data", "System.Data.Common" },
            TargetType = "System.Data.Common.DbCommand",
            TargetMethod = "ExecuteScalar",
            TargetSignatureTypes = new[] { "System.Object" },
            TargetMinimumVersion = "4.0.0",
            TargetMaximumVersion = "4.65535.65535")]
        public static object ExecuteScalar(
            object command,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            const string methodName = "ExecuteScalar";
            Type instanceType = command.GetType();
            Type instrumentedType = command.GetInstrumentedType("System.Data.Common.DbCommand");
            Func<object, object> instrumentedMethod;

            // Use the MethodBuilder to construct a delegate to the original method call
            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<object, object>>
                        .Start(moduleVersionPtr, mdToken, opCode, methodName)
                        .WithConcreteType(instrumentedType)
                        .WithParameters()
                        .WithNamespaceAndNameFilters("System.Object") // Needed for the fallback logic if target method name is overloaded
                        .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    opCode: opCode,
                    mdToken: mdToken,
                    moduleVersionPointer: moduleVersionPtr,
                    methodName: methodName,
                    instanceType: instanceType?.AssemblyQualifiedName,
                    instrumentedType: "System.Data.Common.DbCommand");
                throw;
            }

            // Open a scope, decorate the span, and call the original method
            var dbCommand = command as IDbCommand;
            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, dbCommand, IntegrationName))
            {
                try
                {
                    return instrumentedMethod(command);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Instrumentation wrapper for <see cref="DbCommand.ExecuteScalarAsync(CancellationToken)"/>
        /// </summary>
        /// <param name="command">The object referenced by this in the instrumented method.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationToken"/> value used in the original method call.</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>The value returned by the instrumented method.</returns>
        [InterceptMethod(
            TargetAssemblies = new[] { AdoNetConstants.AssemblyNames.SystemData, AdoNetConstants.AssemblyNames.SystemDataCommon },
            TargetType = DbCommandTypeName,
            TargetSignatureTypes = new[] { "System.Threading.Tasks.Task`1<System.Object>", ClrNames.CancellationToken },
            TargetMinimumVersion = Major4,
            TargetMaximumVersion = Major4)]
        public static object ExecuteScalarAsync(
            object command,
            object cancellationTokenSource,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            var tokenSource = cancellationTokenSource as CancellationTokenSource;
            var cancellationToken = tokenSource?.Token ?? CancellationToken.None;

            return ExecuteScalarAsyncInternal(
                command as DbCommand,
                cancellationToken,
                opCode,
                mdToken,
                moduleVersionPtr);
        }

        private static async Task<object> ExecuteScalarAsyncInternal(
            DbCommand command,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<DbCommand, CancellationToken, Task<object>> instrumentedMethod;
            var instrumentedType = command.GetInstrumentedType(DbCommandTypeName);

            try
            {
                instrumentedMethod =
                    MethodBuilder<Func<DbCommand, CancellationToken, Task<object>>>
                       .Start(moduleVersionPtr, mdToken, opCode, AdoNetConstants.MethodNames.ExecuteScalarAsync)
                       .WithConcreteType(instrumentedType)
                       .WithParameters(cancellationToken)
                       .WithNamespaceAndNameFilters(ClrNames.GenericTask, ClrNames.CancellationToken)
                       .Build();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error resolving {DbCommandTypeName}.{AdoNetConstants.MethodNames.ExecuteScalarAsync}(...)");
                throw;
            }

            using (var scope = ScopeFactory.CreateDbCommandScope(Tracer.Instance, command, IntegrationName))
            {
                try
                {
                    return await instrumentedMethod(command, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scope?.Span.SetException(ex);
                    throw;
                }
            }
        }
    }
}
