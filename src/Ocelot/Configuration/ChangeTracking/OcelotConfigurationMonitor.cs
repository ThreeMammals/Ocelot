using Microsoft.Extensions.Options;
using Ocelot.Configuration.Repository;

namespace Ocelot.Configuration.ChangeTracking
{
    public class OcelotConfigurationMonitor : IOptionsMonitor<IInternalConfiguration>
    {
        private readonly IOcelotConfigurationChangeTokenSource _changeTokenSource;
        private readonly IInternalConfigurationRepository _repo;

        public OcelotConfigurationMonitor(IInternalConfigurationRepository repo, IOcelotConfigurationChangeTokenSource changeTokenSource)
        {
            _changeTokenSource = changeTokenSource;
            _repo = repo;
        }

        public IInternalConfiguration Get(string name)
        {
            return _repo.Get().Data;
        }

        public IDisposable OnChange(Action<IInternalConfiguration, string> listener)
        {
            return _changeTokenSource.ChangeToken.RegisterChangeCallback(_ => listener(CurrentValue, string.Empty), null);
        }

        public IInternalConfiguration CurrentValue => _repo.Get().Data;
    }
}
