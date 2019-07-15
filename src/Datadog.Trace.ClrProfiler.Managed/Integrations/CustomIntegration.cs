#if NETFRAMEWORK

using System;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    public static class CustomIntegration
    {
        [InterceptMethod(
            TargetAssembly = "Samples.CustomInstrumentation",
            TargetType = "Samples.CustomInstrumentation.Program",
            TargetMethod = "Method1",
            TargetSignatureTypes = new[] { ClrNames.Void, ClrNames.String })]
        public static object Wrapper(__arglist)
        {
            var args = new ArgIterator(__arglist);
            var values = new object[args.GetRemainingCount()];

            for (int x = 0; x < values.Length; x++)
            {
                values[x] = TypedReference.ToObject(args.GetNextArg());
            }

            // Scope scope = Start(values);
            // TODO: call original method
            return null;
        }

        public static Scope Start(object[] args)
        {
            return Tracer.Instance.StartActive("TODO");
        }
    }
}

#endif
