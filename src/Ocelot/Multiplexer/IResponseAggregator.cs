using System.Collections.Generic;
using System.Threading.Tasks;

using Ocelot.Configuration;

using Microsoft.AspNetCore.Http;

namespace Ocelot.Multiplexer
{
    public interface IResponseAggregator
    {
        Task Aggregate(Route route, HttpContext originalContext, List<HttpContext> downstreamResponses);
    }
}
