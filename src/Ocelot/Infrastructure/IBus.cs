using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Infrastructure
{
    public interface IBus<T>
    {
        void Subscribe(Action<T> action);   
        Task Publish(T message, int delay);
    }
}
