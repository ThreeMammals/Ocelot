using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;

namespace Ocelot.Multiplexer
{
    public interface IResponseAggregator
    {
        Task Aggregate(Route route, HttpContext originalContext, List<HttpContext> downstreamResponses);
    }
}
