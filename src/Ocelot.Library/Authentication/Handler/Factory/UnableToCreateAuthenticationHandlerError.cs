using Ocelot.Library.Errors;

namespace Ocelot.Library.Authentication.Handler.Factory
{
    public class UnableToCreateAuthenticationHandlerError : Error
    {
        public UnableToCreateAuthenticationHandlerError(string message) 
            : base(message, OcelotErrorCode.UnableToCreateAuthenticationHandlerError)
        {
        }
    }
}
