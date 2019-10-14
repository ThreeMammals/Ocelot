namespace Ocelot.Configuration.ChangeTracking
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Primitives;

    public class OcelotConfigurationChangeToken : IChangeToken
    {
        private static double PollingIntervalSeconds = 1;

        private readonly ICollection<CallbackWrapper> _callbacks = new List<CallbackWrapper>();
        private readonly object _lock = new object();
        private DateTime? _timeChanged;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            lock (_lock)
            {
                var wrapper = new CallbackWrapper(callback, state, _callbacks, _lock);
                _callbacks.Add(wrapper);
                return wrapper;
            }
        }

        public void Activate()
        {
            lock (_lock)
            {
                _timeChanged = DateTime.UtcNow;
                foreach (var wrapper in _callbacks)
                {
                    wrapper.Invoke();
                }
            }
        }

        public bool HasChanged => _timeChanged.HasValue && (DateTime.UtcNow - _timeChanged.Value).TotalSeconds < PollingIntervalSeconds;

        public bool ActiveChangeCallbacks => true;

        private class CallbackWrapper : IDisposable
        {
            private readonly ICollection<CallbackWrapper> _callbacks;
            private readonly object _lock;

            public CallbackWrapper(Action<object> callback, object state, ICollection<CallbackWrapper> callbacks, object lock_)
            {
                _callbacks = callbacks;
                _lock = lock_;
                Callback = callback;
                State = state;
            }

            public void Invoke()
            {
                Callback.Invoke(State);
            }

            public void Dispose()
            {
                lock (_lock)
                {
                    _callbacks.Remove(this);
                }
            }

            public Action<object> Callback { get; }

            public object State { get; }
        }
    }
}
