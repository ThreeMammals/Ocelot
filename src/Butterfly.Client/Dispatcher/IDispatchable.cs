using System;

namespace Butterfly.Client
{
    public interface IDispatchable
    {
        DispatchableToken Token { get; }

        object RawInstance { get; }

        SendState State { get; set; }

        int ErrorCount { get; }

        int Error();

        DateTimeOffset Timestamp { get; }
    }

    public class Dispatchable<T> : IDispatchable
    {
        public DispatchableToken Token { get; }

        public object RawInstance { get; }

        public T Instance { get; }

        public Dispatchable(string token, T instance)
        {
            Token = token;
            RawInstance = Instance = instance;
        }

        private int _counter = 0;

        public SendState State { get; set; } = SendState.Untreated;

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

        public int ErrorCount
        {
            get { return _counter; }
        }

        public int Error()
        {
            return System.Threading.Interlocked.Increment(ref _counter);
        }
    }


    public enum SendState
    {
        Untreated,
        Sending,
        Sended
    }
}