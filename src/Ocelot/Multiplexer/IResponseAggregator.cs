namespace Ocelot.Multiplexer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Configuration;

    using Microsoft.AspNetCore.Http;

    public interface IResponseAggregator
    {
        Task Aggregate(Route route, HttpContext originalContext, List<HttpContext> downstreamResponses);
    }
}
