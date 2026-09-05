namespace Ocelot.Configuration.Repository;

public class InMemoryFileConfigurationPollerOptions : IFileConfigurationPollerOptions
{
    public const int DefaultDelayMilliseconds = 1000;
    public static int DelayMilliseconds { get; set; } = DefaultDelayMilliseconds;

    public int Delay() => DelayMilliseconds;

    public Task<int> DelayAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DelayMilliseconds);

}
