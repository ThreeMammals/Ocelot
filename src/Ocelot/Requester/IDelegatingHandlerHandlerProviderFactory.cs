using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerProviderFactory
    {
        Response<IDelegatingHandlerHandlerProvider> Get(DownstreamReRoute request);
    }
}
