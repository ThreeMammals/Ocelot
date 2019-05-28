using Ocelot.Errors;

namespace Ocelot.Authorisation
{
    public class UserDoesNotHaveClaimError : Error
    {
        public UserDoesNotHaveClaimError(string message)
            : base(message, OcelotErrorCode.UserDoesNotHaveClaimError)
        {
        }
    }
}
