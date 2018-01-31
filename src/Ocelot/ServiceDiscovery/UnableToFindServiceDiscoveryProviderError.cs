using Ocelot.Errors;

namespace Ocelot.ServiceDiscovery
{
    public class UnableToFindServiceDiscoveryProviderError : Error
    {
        public UnableToFindServiceDiscoveryProviderError(string message) 
            : base(message, OcelotErrorCode.UnableToFindServiceDiscoveryProviderError)
        {
        }
    }
}