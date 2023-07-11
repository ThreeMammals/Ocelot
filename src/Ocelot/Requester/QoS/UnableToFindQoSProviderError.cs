using Ocelot.Errors;

namespace Ocelot.Requester.QoS
{
    public class UnableToFindQoSProviderError : Error
    {
        public UnableToFindQoSProviderError(string message)
            : base(message, OcelotErrorCode.UnableToFindQoSProviderError, 404)
        {
        }
    }
}
