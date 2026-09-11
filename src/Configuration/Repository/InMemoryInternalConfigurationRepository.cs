using Ocelot.Configuration.ChangeTracking;

namespace Ocelot.Configuration.Repository;

public class InMemoryInternalConfigurationRepository : IInternalConfigurationRepository
{
#if NET9_0_OR_GREATER
    private static readonly Lock Locker = new();
#else
    private static readonly object Locker = new();
#endif

    private IInternalConfiguration _internalConfiguration;
    private readonly IOcelotConfigurationChangeTokenSource _changeTokenSource;

    public InMemoryInternalConfigurationRepository(IOcelotConfigurationChangeTokenSource changeTokenSource)
    {
        _changeTokenSource = changeTokenSource;
    }

    public IInternalConfiguration Get()
    {
        lock (Locker)
        {
            return _internalConfiguration;
        }
    }

    public string AddOrReplace(IInternalConfiguration internalConfiguration)
    {
        lock (Locker)
        {
            _internalConfiguration = internalConfiguration;
        }

        _changeTokenSource.Activate();
        return string.Empty; 
    }
}
