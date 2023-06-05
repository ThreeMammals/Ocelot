using Ocelot.Errors;

namespace Ocelot.Authorization
{
    public class ClaimValueNotAuthorizedError : Error
    {
        public ClaimValueNotAuthorizedError(string message)
            : base(message, OcelotErrorCode.ClaimValueNotAuthorizedError, 403)
        {
        }
    }
}
