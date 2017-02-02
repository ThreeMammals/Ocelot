using Ocelot.Errors;

namespace Ocelot.ServiceDiscovery
{
    public class UnableToFindServiceProviderError : Error
    {
        public UnableToFindServiceProviderError(string message) 
            : base(message, OcelotErrorCode.UnableToFindServiceProviderError)
        {
        }
    }
}