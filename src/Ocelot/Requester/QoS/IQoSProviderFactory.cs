using Ocelot.Configuration;
using Ocelot.LoadBalancer.LoadBalancers;

namespace Ocelot.Requester.QoS
{
    public interface IQoSProviderFactory
    {
        IQoSProvider Get(DownstreamReRoute reRoute);
    }
}
