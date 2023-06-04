using Microsoft.Extensions.Primitives;

namespace Ocelot.Configuration.ChangeTracking
{
    /// <summary>
    /// <see cref="IChangeToken" /> source which is activated when Ocelot's configuration is changed.
    /// </summary>
    public interface IOcelotConfigurationChangeTokenSource
    {
        IChangeToken ChangeToken { get; }

        void Activate();
    }
}
