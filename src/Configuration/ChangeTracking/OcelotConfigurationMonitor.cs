using Microsoft.Extensions.Options;
using Ocelot.Configuration.Repository;

namespace Ocelot.Configuration.ChangeTracking;

public class OcelotConfigurationMonitor : IOptionsMonitor<IInternalConfiguration>
{
    private readonly IInternalConfigurationRepository _repo;
    private readonly IOcelotConfigurationChangeTokenSource _changeTokenSource;

    public OcelotConfigurationMonitor(IInternalConfigurationRepository repo, IOcelotConfigurationChangeTokenSource changeTokenSource)
    {
        ArgumentNullException.ThrowIfNull(repo);
        ArgumentNullException.ThrowIfNull(changeTokenSource);
        _repo = repo;
        _changeTokenSource = changeTokenSource;
    }

    public IInternalConfiguration Get(string name) => _repo.Get();

    public IDisposable OnChange(Action<IInternalConfiguration, string> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        return _changeTokenSource.ChangeToken.RegisterChangeCallback(_ => listener(CurrentValue, string.Empty), null);
    }

    public IInternalConfiguration CurrentValue => _repo.Get();
}
