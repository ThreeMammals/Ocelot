using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.LoadBalancer.Errors;

public class CouldNotFindLoadBalancerCreatorError : Error
{
    public CouldNotFindLoadBalancerCreatorError(string message)
        : base(message, OcelotErrorCode.CouldNotFindLoadBalancerCreator, StatusCodes.Status404NotFound)
    {
    }
}
