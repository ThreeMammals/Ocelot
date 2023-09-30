using Ocelot.Configuration;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public interface IDelegatingHandlerHandlerFactory
    {
        Response<List<Func<DelegatingHandler>>> Get(DownstreamRoute downstreamRoute);
    }
}
