using System;
using Datadog.Trace;
using Datadog.Trace.ClrProfiler.Integrations;

namespace Samples.CustomInstrumentation
{
    internal static class Program
    {
        public static void Main()
        {
            Method1("message", 5);

            Method2(__arglist("message", 5));
        }

        public static void Method1(string s, int i)
        {
            Console.WriteLine("{0} {1}", s, i);
        }

        public static void Method2(__arglist)
        {
            var args = new ArgIterator(__arglist);
            var values = new object[args.GetRemainingCount()];

            for (int x = 0; x < values.Length; x++)
            {
                values[x] = TypedReference.ToObject(args.GetNextArg());
            }

            Console.WriteLine("{0} {1}", values);
        }

        public static void Method3()
        {
            Span span = Tracer.Instance.StartSpan("method.call");

            try
            {
                Console.WriteLine("Method3()");
            }
            catch (Exception ex)
            {
                span?.SetException(ex);
                throw;
            }
            finally
            {
                span?.Dispose();
            }
        }
    }
}
