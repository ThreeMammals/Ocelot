using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration.Creator
{
    public interface IOcelotConfigurationCreator
    {
        Response<IOcelotConfiguration> Create();
    }
}