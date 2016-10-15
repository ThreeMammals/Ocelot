using Ocelot.Library.Infrastructure.Errors;

namespace Ocelot.Library.Infrastructure.Middleware
{
    public class UnauthenticatedError : Error
    {
        public UnauthenticatedError(string message) : base(message, OcelotErrorCode.UnauthenticatedError)
        {
        }
    }
}
