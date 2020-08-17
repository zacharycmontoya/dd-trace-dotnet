using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent.MessagePack;
using Datadog.Trace.Vendors.MessagePack;
using Datadog.Trace.Vendors.MessagePack.Formatters;

namespace Datadog.Trace.Agent
{
    internal class BinaryBuffer
    {
        private readonly ConcurrentQueue<Span[]> _pendingTraces = new ConcurrentQueue<Span[]>();
        private readonly IMessagePackFormatter<Span[]> _spanFormatter;
        private readonly FormatterResolverWrapper _formatterResolver = new FormatterResolverWrapper(SpanFormatterResolver.Instance);

        private Task _backgroundThread;
        private byte[] _buffer = new byte[1024 * 1024];
        private int _offset = 0;
        private int _count;

        private IApi _api;

        private bool _pendingFlush;

        public BinaryBuffer(IApi api)
        {
            _api = api;
            _spanFormatter = new ArrayFormatter<Span>();
            _backgroundThread = Task.Factory.StartNew(ProcessTraces, TaskCreationOptions.LongRunning)
                .ContinueWith(t => Console.WriteLine(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Push(Span[] traces)
        {
            _pendingTraces.Enqueue(traces);
        }

        public void Flush()
        {
            Volatile.Write(ref _pendingFlush, true);
        }

        private void ProcessTraces()
        {
            while (true)
            {
                while (_pendingTraces.TryDequeue(out var traces))
                {
                    _offset += _spanFormatter.Serialize(ref _buffer, _offset, traces, _formatterResolver);
                    _count++;

                    if (Volatile.Read(ref _pendingFlush))
                    {
                        Volatile.Write(ref _pendingFlush, false);

                        _api.SendTracesAsync(stream => WriteTraces(stream), _count).GetAwaiter().GetResult();

                        _offset = 0;
                        _count = 0;
                    }
                }

                if (Volatile.Read(ref _pendingFlush))
                {
                    Volatile.Write(ref _pendingFlush, false);

                    _api.SendTracesAsync(stream => WriteTraces(stream), _count).GetAwaiter().GetResult();

                    _offset = 0;
                    _count = 0;
                }

                Thread.Sleep(100);
            }
        }

        private void WriteTraces(Stream destinationStream)
        {
            MessagePackBinary.WriteArrayHeader(destinationStream, _count);

            destinationStream.Write(_buffer, 0, _offset);
        }
    }
}
