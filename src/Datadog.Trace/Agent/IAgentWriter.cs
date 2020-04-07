using System.Collections.Generic;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IAgentWriter
    {
        void WriteTrace(IReadOnlyCollection<Span> trace);

        Task FlushAndCloseAsync();

        void OverrideApi(IApi api);
    }
}
