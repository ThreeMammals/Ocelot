using Ocelot.Errors;

namespace Ocelot.LoadBalancer.LoadBalancers
{
    public class CouldNotFindLoadBalancerCreator : Error
    {
        public CouldNotFindLoadBalancerCreator(string message)
            : base(message, OcelotErrorCode.CouldNotFindLoadBalancerCreator, 404)
        {
        }
    }
}
