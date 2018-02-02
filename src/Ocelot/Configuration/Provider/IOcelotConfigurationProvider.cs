using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    public interface IOcelotConfigurationProvider
    {
        Task<Response<IOcelotConfiguration>> Get();
    }
}
