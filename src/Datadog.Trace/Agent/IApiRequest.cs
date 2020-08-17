using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.Agent.MessagePack;
using Datadog.Trace.Headers;

namespace Datadog.Trace.Agent
{
    internal interface IApiRequest
    {
        void AddHeader(string name, string value);

        Task<IApiResponse> PostAsync(Span[][] traces, FormatterResolverWrapper formatterResolver);

        Task<IApiResponse> PostAsync(Action<Stream> writer);
    }
}
