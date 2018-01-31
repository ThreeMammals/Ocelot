using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public interface IOcelotConfigurationRepository
    {
        Task<Response<IOcelotConfiguration>> Get();
        Task<Response> AddOrReplace(IOcelotConfiguration ocelotConfiguration);
    }
}