using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration.Repository
{
    public interface IOcelotConfigurationRepository
    {
        Response<IOcelotConfiguration> Get();
        Response AddOrReplace(IOcelotConfiguration ocelotConfiguration);
    }
}