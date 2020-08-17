using System;
using System.IO;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IApi
    {
        Task<bool> SendTracesAsync(Span[][] traces);

        Task<bool> SendTracesAsync(Action<Stream> writer, int count);
    }
}
