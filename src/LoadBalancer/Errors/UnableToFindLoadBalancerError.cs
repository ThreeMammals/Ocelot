using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.LoadBalancer.Errors;

public class UnableToFindLoadBalancerError : Error
{
    public UnableToFindLoadBalancerError(string message)
        : base(message, OcelotErrorCode.UnableToFindLoadBalancerError, StatusCodes.Status404NotFound)
    {
    }
}
