using System.Collections.Generic;
using System.Threading.Tasks;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace.Agent
{
    internal interface IApi
    {
        Task SendTracesAsync(IList<List<ISpanData>> traces);
    }
}
