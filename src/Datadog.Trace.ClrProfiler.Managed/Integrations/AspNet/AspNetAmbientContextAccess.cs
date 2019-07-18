#if !NETSTANDARD2_0

using System;
using System.Web;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal class AspNetAmbientContextAccess : IAmbientContextAccess
    {
        private readonly string _reservedHttpContextKey = $"__Data_dog_scope__{Guid.NewGuid()}";

        public int Priority => 10;

        public Scope GetActiveScope()
        {
            var activeScope = HttpContext.Current?.Items[_reservedHttpContextKey] as Scope;
            return activeScope;
        }

        public bool TrySetActiveScope(Scope scope)
        {
            try
            {
                var httpContext = HttpContext.Current;
                if (httpContext != null)
                {
                    httpContext.Items[_reservedHttpContextKey] = scope;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif