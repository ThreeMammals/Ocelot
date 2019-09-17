using Ocelot.Errors;

namespace Ocelot.Authorisation
{
    public class UnauthorisedError : Error
    {
        public UnauthorisedError(string message)
            : base(message, OcelotErrorCode.UnauthorizedError)
        {
        }
    }
}
