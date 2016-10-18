using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    public interface IOcelotConfigurationCreator
    {
        Response<IOcelotConfiguration> Create();
    }
}