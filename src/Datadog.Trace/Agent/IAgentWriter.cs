using System.Collections.Generic;
using System.Threading.Tasks;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace.Agent
{
    internal interface IAgentWriter
    {
        void WriteTrace(List<ISpanData> trace);

        Task FlushAndCloseAsync();
    }
}
