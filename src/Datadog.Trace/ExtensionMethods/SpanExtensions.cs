using System;
using System.Data;
using System.Data.Common;

namespace Datadog.Trace.ExtensionMethods
{
    /// <summary>
    /// Extension methods for the <see cref="Span"/> class.
    /// </summary>
    public static class SpanExtensions
    {
        private static readonly string[] DatabaseNameKeys =
        {
            "Database",
            "Initial Catalog",
            "InitialCatalog"
        };

        private static readonly string[] DatabaseUserKeys =
        {
            "User ID",
            "UserID"
        };

        private static readonly string[] DatabaseHostKeys =
        {
            "Server",
            "Data Source",
            "DataSource",
            "Network Address",
            "NetworkAddress",
            "Address",
            "Addr",
            "Host"
        };

        /// <summary>
        /// Sets the sampling priority for the trace that contains the specified <see cref="Span"/>.
        /// </summary>
        /// <param name="span">A span that belongs to the trace.</param>
        /// <param name="samplingPriority">The new sampling priority for the trace.</param>
        public static void SetTraceSamplingPriority(this Span span, SamplingPriority samplingPriority)
        {
            if (span == null) { throw new ArgumentNullException(nameof(span)); }

            if (span.Context.TraceContext != null)
            {
                span.Context.TraceContext.SamplingPriority = samplingPriority;
            }
        }

        /// <summary>
        /// Adds standard tags to a span with values taken from the specified <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="span">The span to add the tags to.</param>
        /// <param name="command">The db command to get tags values from.</param>
        public static void AddTagsFromDbCommand(this Span span, IDbCommand command)
        {
            span.ResourceName = command.CommandText;
            span.Type = SpanTypes.Sql;

            // parse the connection string
            var builder = new DbConnectionStringBuilder
                          {
                              ConnectionString = command.Connection.ConnectionString
                          };

            string database = GetConnectionStringValue(builder, DatabaseNameKeys);
            span.SetTag(Tags.DbName, database);

            string user = GetConnectionStringValue(builder, DatabaseUserKeys);
            span.SetTag(Tags.DbUser, user);

            string server = GetConnectionStringValue(builder, DatabaseHostKeys);
            span.SetTag(Tags.OutHost, server);
        }

        internal static void DecorateWebServerSpan(
            this Span span,
            string resourceName,
            string method,
            string host,
            string httpUrl)
        {
            span.Type = SpanTypes.Web;
            span.ResourceName = resourceName?.Trim();
            span.SetTag(Tags.SpanKind, SpanKinds.Server);
            span.SetTag(Tags.HttpMethod, method);
            span.SetTag(Tags.HttpRequestHeadersHost, host);
            span.SetTag(Tags.HttpUrl, httpUrl);
            span.SetTag(Tags.Language, TracerConstants.Language);
        }

        private static string GetConnectionStringValue(DbConnectionStringBuilder builder, string[] names)
        {
            foreach (string name in names)
            {
                if (builder.TryGetValue(name, out object valueObj) &&
                    valueObj is string value)
                {
                    return value;
                }
            }

            return null;
        }
    }
}
