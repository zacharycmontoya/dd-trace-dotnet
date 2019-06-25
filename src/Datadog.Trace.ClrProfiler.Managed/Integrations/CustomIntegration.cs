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
            var iterator = new ArgIterator(__arglist);
            int count = iterator.GetRemainingCount();

            return null;
        }
    }
}

#endif
