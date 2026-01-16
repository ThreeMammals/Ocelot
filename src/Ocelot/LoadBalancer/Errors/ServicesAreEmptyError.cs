using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.LoadBalancer.Errors;

public class ServicesAreEmptyError : Error
{
    public ServicesAreEmptyError(string message)
        : base(message, OcelotErrorCode.ServicesAreEmptyError, StatusCodes.Status404NotFound)
    {
    }
}
