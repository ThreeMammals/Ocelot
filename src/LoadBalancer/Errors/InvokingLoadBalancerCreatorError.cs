using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.LoadBalancer.Errors;

public class InvokingLoadBalancerCreatorError : Error
{
    public InvokingLoadBalancerCreatorError(Exception e)
        : base($"Error when invoking user provided load balancer creator function, Message: {e.Message}, StackTrace: {e.StackTrace}",
            OcelotErrorCode.ErrorInvokingLoadBalancerCreator,
            StatusCodes.Status500InternalServerError)
    {
    }
}
