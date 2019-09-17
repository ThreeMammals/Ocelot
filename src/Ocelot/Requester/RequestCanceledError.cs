using Ocelot.Errors;

namespace Ocelot.Requester
{
    public class RequestCanceledError : Error
    {
        public RequestCanceledError(string message) : base(message, OcelotErrorCode.RequestCanceled)
        {
        }
    }
}
