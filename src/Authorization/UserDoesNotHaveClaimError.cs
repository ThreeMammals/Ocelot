using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Authorization;

public class UserDoesNotHaveClaimError : Error
{
    public UserDoesNotHaveClaimError(string message)
        : base(message, OcelotErrorCode.UserDoesNotHaveClaimError, StatusCodes.Status403Forbidden)
    {
    }
}
