namespace Ocelot.Infrastructure;

public class InMemoryBus<T> : IBus<T>
{
    private readonly BlockingCollection<DelayedMessage<T>> _queue;
    private readonly List<Action<T>> _subscriptions;
    private readonly Thread _processing;

    public InMemoryBus()
    {
        _queue = new BlockingCollection<DelayedMessage<T>>();
        _subscriptions = new List<Action<T>>();
        _processing = new Thread(StartProcessing)
        {
            IsBackground = true
        };
        _processing.Start();
    }

    public void Subscribe(Action<T> action)
    {
        _subscriptions.Add(action);
    }

    public void Publish(T message, int delay)
    {
        var delayed = new DelayedMessage<T>(message, delay);
        _queue.Add(delayed);
    }

    // For delegate void ThreadStart();
    private void StartProcessing()
        => Process(CancellationToken.None).GetAwaiter().GetResult();

    private async Task Process(CancellationToken token = default)
    {
        foreach (var delayedMessage in _queue.GetConsumingEnumerable(token))
        {
            await Task.Delay(delayedMessage.Delay, token);
            foreach (var subscription in _subscriptions)
            {
                subscription(delayedMessage.Message);
            }
        }
    }
}
