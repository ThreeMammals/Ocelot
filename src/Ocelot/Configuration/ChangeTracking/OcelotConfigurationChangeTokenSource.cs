using Microsoft.Extensions.Primitives;

namespace Ocelot.Configuration.ChangeTracking
{
    public class OcelotConfigurationChangeTokenSource : IOcelotConfigurationChangeTokenSource
    {
        private readonly OcelotConfigurationChangeToken _changeToken = new();

        public IChangeToken ChangeToken => _changeToken;

        public void Activate()
        {
            _changeToken.Activate();
        }
    }
}
