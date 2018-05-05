using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ocelot.Infrastructure
{
    public class InMemoryBus<T> : IBus<T>
    {
        private readonly BlockingCollection<T> _queue;
        private readonly List<Action<T>> _subscriptions;
        private Thread _processing;

        public InMemoryBus()
        {
            _queue = new BlockingCollection<T>();
            _subscriptions = new List<Action<T>>();
            _processing = new Thread(Process);
            _processing.Start();
        }

        public void Subscribe(Action<T> action)
        {
            _subscriptions.Add(action);
        }

        public async Task Publish(T message, int delay)
        {
            await Task.Delay(delay);
            _queue.Add(message);
        }

        private void Process()
        {
            foreach(var message in _queue.GetConsumingEnumerable())
            {
                foreach(var subscription in _subscriptions)
                {
                    subscription(message);
                }
            }
        }
    }
}
