namespace Ocelot.Multiplexer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;

    using Configuration;

    public interface IResponseAggregator
    {
        Task Aggregate(Route route, HttpContext originalContext, List<HttpContext> downstreamResponses);
    }
}
