using System;
using System.Buffers;

namespace Datadog.Trace.Collections
{
    internal struct RentedArray<T> : IDisposable
    {
        public static readonly RentedArray<T> Empty = default;

        private readonly bool _clearOnReturn;
        private readonly int _usedLength;

        public RentedArray(int minimumLength, bool clearOnReturn = false)
        {
            _usedLength = minimumLength;
            _clearOnReturn = clearOnReturn;

            FullArray = ArrayPool<T>.Shared.Rent(minimumLength);
        }

        public T[] FullArray { get; }

        public ArraySegment<T> Segment => new ArraySegment<T>(FullArray, 0, _usedLength);

        public void Dispose()
        {
            if (FullArray != null)
            {
                ArrayPool<T>.Shared.Return(FullArray, _clearOnReturn);
            }
        }
    }
}
