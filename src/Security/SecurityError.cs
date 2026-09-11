using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Security;

public class SecurityError : Error
{
    public SecurityError(string message)
        : base(message, OcelotErrorCode.SecurityError, StatusCodes.Status403Forbidden)
    { }

    public SecurityError(string message, int statusCode)
        : base(message, OcelotErrorCode.SecurityError, statusCode)
    { }
}
