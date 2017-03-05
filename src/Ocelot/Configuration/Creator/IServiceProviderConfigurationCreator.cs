using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public interface IServiceProviderConfigurationCreator
    {
        ServiceProviderConfiguration Create(FileReRoute fileReRoute, FileGlobalConfiguration globalConfiguration);
    }
}