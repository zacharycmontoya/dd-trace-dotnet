using System;
using System.Collections.Generic;
using System.Threading;

namespace Datadog.Trace.Collections
{
    internal class DoubleBuffer<T> : IBuffer<T>
    {
        private static readonly IReadOnlyList<T> EmptyBuffer = new T[0];

        private IBuffer<T> _frontBuffer;
        private IBuffer<T> _backBuffer;

        public DoubleBuffer(Func<IBuffer<T>> bufferFactory)
        {
            if (bufferFactory == null)
            {
                throw new ArgumentNullException(nameof(bufferFactory));
            }

            _frontBuffer = bufferFactory();
            _backBuffer = bufferFactory();
        }

        public int Count => _frontBuffer.Count;

        public bool Add(T item)
        {
            lock (_frontBuffer)
            {
                return _frontBuffer.Add(item);
            }
        }

        public IReadOnlyCollection<T> GetAll()
        {
            if (_frontBuffer.Count == 0)
            {
                // don't lock or allocate anything if the current buffer is empty
                return EmptyBuffer;
            }

            // clear the back buffer before swapping
            _backBuffer.Clear();

            // swap the front buffer with the empty back buffer
            IBuffer<T> buffer = Interlocked.Exchange(ref _frontBuffer, _backBuffer);
            _backBuffer = buffer;

            // Consumer is now free to use the back buffer
            // while producers write to the front buffer.
            // NOTE: the consumer needs to be done using
            // the returned value before calling GetAll() again.
            return buffer.GetAll();
        }

        public void Clear()
        {
            lock (_frontBuffer)
            {
                _frontBuffer.Clear();
            }
        }
    }
}
