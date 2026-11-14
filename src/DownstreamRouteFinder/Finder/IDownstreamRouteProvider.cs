using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.DownstreamRouteFinder.Finder;

public interface IDownstreamRouteProvider
{
    [Obsolete(DownstreamRouteFinder.Use_Get_HttpRequest_IEnumerable_method)]
    Response<Route> Get(string upstreamUrlPath, string upstreamQueryString, string upstreamHttpMethod,
        IInternalConfiguration configuration, string upstreamHost, IHeaderDictionary upstreamHeaders);

    Route Get(HttpRequest request, out IEnumerable<Error> errors);
}
