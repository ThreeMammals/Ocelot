using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public interface IOcelotConfigurationRepository
    {
        Response<IOcelotConfiguration> Get();
        Response AddOrReplace(IOcelotConfiguration ocelotConfiguration);
    }
}
