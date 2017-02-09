using Ocelot.Errors;

namespace Ocelot.Authentication.Handler.Factory
{
    public class UnableToCreateAuthenticationHandlerError : Error
    {
        public UnableToCreateAuthenticationHandlerError(string message) 
            : base(message, OcelotErrorCode.UnableToCreateAuthenticationHandlerError)
        {
        }
    }
}
