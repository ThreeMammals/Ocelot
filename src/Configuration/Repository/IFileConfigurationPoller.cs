using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;

namespace Ocelot.Configuration.Repository;

/// <summary>
/// Defines polling of configuration as a hosted service.
/// </summary>
/// <remarks>
/// Feature: <see cref="IOcelotBuilder.AddConfigurationPoller()"/>.<br/>
/// Feature PR: <see href="https://github.com/ThreeMammals/Ocelot/pull/157/">157</see>.
/// </remarks>
public interface IFileConfigurationPoller : IHostedService, IDisposable
{
    void Poll();
    Task PollAsync(CancellationToken cancellationToken = default);
}
