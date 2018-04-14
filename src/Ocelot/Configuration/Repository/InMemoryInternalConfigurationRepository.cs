using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class InMemoryInternalConfigurationRepository : IInternalConfigurationRepository
    {
        private static readonly object LockObject = new object();

        private IInternalConfiguration _internalConfiguration;

        public Response<IInternalConfiguration> Get()
        {
            return new OkResponse<IInternalConfiguration>(_internalConfiguration);
        }

        public Response AddOrReplace(IInternalConfiguration internalConfiguration)
        {
            lock (LockObject)
            {
                _internalConfiguration = internalConfiguration;
            }

            return new OkResponse();
        }
    }
}
