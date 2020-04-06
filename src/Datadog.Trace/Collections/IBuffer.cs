using System;
using System.Collections.Generic;

namespace Datadog.Trace.Collections
{
    internal interface IBuffer<T>
    {
        int Count { get; }

        bool Add(T item);

        IReadOnlyCollection<T> GetAll();

        void Clear();
    }
}
