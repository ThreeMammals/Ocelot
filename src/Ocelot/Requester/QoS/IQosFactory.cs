using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester.QoS
{
    public interface IQoSFactory
    {
        Response<DelegatingHandler> Get(DownstreamRoute request);
    }
}
