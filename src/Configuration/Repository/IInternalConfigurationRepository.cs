namespace Ocelot.Configuration.Repository;

public interface IInternalConfigurationRepository
{
    IInternalConfiguration Get();

    string AddOrReplace(IInternalConfiguration configuration);
}
