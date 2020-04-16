using System.Collections.Generic;
using Datadog.Trace;
using Datadog.Trace.ExtensionMethods;
using JetBrains.Profiler.Api;

namespace ConsoleApp1
{
    public static class Program
    {
        public const int Loops = 10;
        public const int TracesPerLoop = 100;
        public const int SpansPerTrace = 10;
        public const int TagsPerSpan = 10;

        public static KeyValuePair<string, string>[] _tags = new KeyValuePair<string, string>[TagsPerSpan];

        public static void Main()
        {
            MemoryProfiler.CollectAllocations(false);

            for (int i = 0; i < TagsPerSpan; i++)
            {
                _tags[i] = new KeyValuePair<string, string>($"key{i}", $"value{i}");
            }

            MemoryProfiler.ForceGc();
            MemoryProfiler.CollectAllocations(true);

            for (int loop = 0; loop < Loops; loop++)
            {
                for (int traceIndex = 0; traceIndex < TracesPerLoop; traceIndex++)
                {
                    CreateTrace();
                }

                Tracer.Instance.FlushAsync().GetAwaiter().GetResult();
                MemoryProfiler.GetSnapshot();
            }
        }

        private static void CreateTrace()
        {
            using (Scope rootScope = Tracer.Instance.StartActive("root"))
            {
                Span rootSpan = rootScope.Span;
                rootSpan.Type = SpanTypes.Custom;
                AddTags(rootSpan, TagsPerSpan);
                rootSpan.SetTraceSamplingPriority(SamplingPriority.UserReject);

                for (int spanIndex = 0; spanIndex < SpansPerTrace - 1; spanIndex++)
                {
                    using (Scope childScope = Tracer.Instance.StartActive("child"))
                    {
                        Span childSpan = childScope.Span;
                        childSpan.Type = SpanTypes.Custom;
                        AddTags(rootSpan, TagsPerSpan);
                    }
                }
            }
        }

        private static void AddTags(Span span, int count)
        {
            for (int i = 0; i < count; i++)
            {
                span.SetTag(_tags[i].Key, _tags[i].Value);
            }
        }
    }
}
