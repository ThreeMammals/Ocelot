using Ocelot.Library.Infrastructure.Errors;

namespace Ocelot.Library.Infrastructure.Authentication
{
    public class UnableToCreateAuthenticationHandlerError : Error
    {
        public UnableToCreateAuthenticationHandlerError(string message) 
            : base(message, OcelotErrorCode.UnableToCreateAuthenticationHandlerError)
        {
        }
    }
}
