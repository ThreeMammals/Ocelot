using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration.Provider
{
    public interface IOcelotConfigurationProvider
    {
        Response<IOcelotConfiguration> Get();
    }
}
