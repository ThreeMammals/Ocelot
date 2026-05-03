using Microsoft.Extensions.Hosting;

namespace Ocelot.Configuration.Repository;

public interface IFileConfigurationPoller : IHostedService, IDisposable
{
    void Poll();
    Task PollAsync(CancellationToken cancellationToken = default);
}
