using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerHouse
    {
        Response<IDelegatingHandlerHandlerProvider> Get(DownstreamReRoute request);
    }
}
