using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Authorization;

public class UnauthorizedError : Error
{
    public UnauthorizedError(string message)
        : base(message, OcelotErrorCode.UnauthorizedError, StatusCodes.Status403Forbidden)
    {
    }
}
