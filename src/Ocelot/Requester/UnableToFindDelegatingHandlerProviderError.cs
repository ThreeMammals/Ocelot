using Ocelot.Errors;

namespace Ocelot.Requester
{
    public class UnableToFindDelegatingHandlerProviderError : Error
    {
        public UnableToFindDelegatingHandlerProviderError(string message)
            : base(message, OcelotErrorCode.UnableToFindDelegatingHandlerProviderError)
        {
        }
    }
}
