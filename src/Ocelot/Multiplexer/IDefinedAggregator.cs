using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;

namespace Ocelot.Multiplexer
{
    public interface IDefinedAggregator
    {
        Task<DownstreamResponse> Aggregate(List<HttpContext> responses);
    }
}
