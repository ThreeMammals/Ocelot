using Ocelot.Errors;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class ErrorInvokingLoadBalancerCreator : Error
    {
        public ErrorInvokingLoadBalancerCreator(Exception e) : base($"Error when invoking user provided load balancer creator function, Message: {e.Message}, StackTrace: {e.StackTrace}", OcelotErrorCode.ErrorInvokingLoadBalancerCreator, 500)
        {
        }
    }
}
