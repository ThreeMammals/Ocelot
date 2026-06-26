using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.Security;

public class IpSecurityError : Error
{
    public IpSecurityError(string message)
        : base(message, OcelotErrorCode.IpSecurityError, StatusCodes.Status403Forbidden)
    { }
}
