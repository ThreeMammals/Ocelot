using System.Threading.Tasks;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class InMemoryOcelotConfigurationRepository : IOcelotConfigurationRepository
    {
        private static readonly object LockObject = new object();

        private IOcelotConfiguration _ocelotConfiguration;

        public async Task<Response<IOcelotConfiguration>> Get()
        {
            return new OkResponse<IOcelotConfiguration>(_ocelotConfiguration);
        }

        public async Task<Response> AddOrReplace(IOcelotConfiguration ocelotConfiguration)
        {
            lock (LockObject)
            {
                _ocelotConfiguration = ocelotConfiguration;
            }

            return new OkResponse();
        }
    }
}