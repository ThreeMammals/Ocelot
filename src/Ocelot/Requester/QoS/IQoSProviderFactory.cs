using Ocelot.Configuration;

namespace Ocelot.Requester.QoS
{
    public interface IQoSProviderFactory
    {
        IQoSProvider Get(DownstreamReRoute reRoute);
    }
}
