using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    public interface IOcelotConfigurationProvider
    {
        Response<IOcelotConfiguration> Get();
    }
}
