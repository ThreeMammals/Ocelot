using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.LoadBalancer.Errors;

public class ServicesAreNullError : Error
{
    public ServicesAreNullError(string message)
        : base(message, OcelotErrorCode.ServicesAreNullError, StatusCodes.Status404NotFound)
    {
    }
}
