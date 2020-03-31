using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Sample.Empty
{
    public static class Program
    {
        [LoaderOptimization(LoaderOptimization.MultiDomain)]
        public static async Task Main()
        {
            var http = new HttpClient();
            await http.GetAsync("https://www.example.com");

            var instrumentationType = Type.GetType("Datadog.Trace.ClrProfiler.Instrumentation, Datadog.Trace.ClrProfiler.Managed");
            var profilerAttached = instrumentationType?.GetProperty("ProfilerAttached", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ?? false;
            var tracerAssemblyLocation = Type.GetType("Datadog.Trace.Tracer, Datadog.Trace")?.Assembly.Location;
            var clrProfilerAssemblyLocation = instrumentationType?.Assembly.Location;

            Console.WriteLine($"Profiler attached: {profilerAttached}");
            Console.WriteLine(tracerAssemblyLocation ?? "Datadog.Trace.dll not loaded");
            Console.WriteLine(clrProfilerAssemblyLocation ?? "Datadog.Trace.ClrProfiler.Managed.dll not loaded");
            Console.WriteLine();

            var prefixes = new[] { "COR_", "CORECLR_", "DD_", "DATADOG_" };

            var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                          from prefix in prefixes
                          let key = (envVar.Key as string)?.ToUpperInvariant()
                          let value = envVar.Value as string
                          where key.StartsWith(prefix)
                          orderby key
                          select new KeyValuePair<string, string>(key, value);

            foreach (var envVar in envVars)
            {
                Console.WriteLine($"{envVar.Key}={envVar.Value}");
            }

            Console.WriteLine();
        }
    }
}
