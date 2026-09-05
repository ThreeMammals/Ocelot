namespace Ocelot.Configuration.Repository;

public interface IFileConfigurationPollerOptions
{
    int Delay();
    Task<int> DelayAsync(CancellationToken cancellationToken = default);
}
