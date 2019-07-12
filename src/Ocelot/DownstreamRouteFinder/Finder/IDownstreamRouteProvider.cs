using Ocelot.Configuration;
using Ocelot.Responses;
using Microsoft.AspNetCore.Http;

namespace Ocelot.DownstreamRouteFinder.Finder
{
    public interface IDownstreamRouteProvider
    {
        Response<DownstreamRoute> Get(
            string upstreamUrlPath,
            string upstreamQueryString,
            string upstreamHttpMethod,
            IInternalConfiguration configuration,
            string upstreamHost,
            IHeaderDictionary requestHeaders);
    }
}
