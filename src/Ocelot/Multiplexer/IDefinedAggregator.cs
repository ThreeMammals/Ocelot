using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Multiplexer
{
    public interface IDefinedAggregator
    {
        Task<DownstreamResponse> Aggregate(List<HttpContext> responses);
    }
}
