using Ocelot.Errors;

namespace Ocelot.Authorization
{
    public class UserDoesNotHaveClaimError : Error
    {
        public UserDoesNotHaveClaimError(string message)
            : base(message, OcelotErrorCode.UserDoesNotHaveClaimError, 403)
        {
        }
    }
}
