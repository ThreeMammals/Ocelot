using Ocelot.Configuration;
using Ocelot.Responses;
using System.Net.Http;

namespace Ocelot.Requester.QoS
{
    public interface IQoSFactory
    {
        Response<DelegatingHandler> Get(DownstreamRoute request);
    }
}
