using Ocelot.Errors;

namespace Ocelot.ServiceDiscovery.Errors
{
    public class UnableToFindServiceDiscoveryProviderError : Error
    {
        public UnableToFindServiceDiscoveryProviderError(string message) 
            : base(message, OcelotErrorCode.UnableToFindServiceDiscoveryProviderError)
        {
        }
    }
}
