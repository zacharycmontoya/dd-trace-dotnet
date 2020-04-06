using System;
using System.Collections.Generic;

namespace Datadog.Trace.Collections
{
    internal class SingleBuffer<T> : IBuffer<T>
    {
        private int _lastDroppedIndex;

        public SingleBuffer(int capacity)
        {
            Items = new T[capacity];
        }

        public int Count { get; private set; }

        public T[] Items { get; }

        public bool Add(T item)
        {
            if (Count < Items.Length)
            {
                Items[Count++] = item;
                return true;
            }

            // buffer is full, replace (drop) the oldest item
            _lastDroppedIndex = (_lastDroppedIndex + 1) % Items.Length;
            Items[_lastDroppedIndex] = item;
            return false;
        }

        public IReadOnlyCollection<T> GetAll()
        {
            return new ArraySegment<T>(Items, 0, Count);
        }

        public void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(Items, 0, Count);
                Count = 0;
            }
        }
    }
}
