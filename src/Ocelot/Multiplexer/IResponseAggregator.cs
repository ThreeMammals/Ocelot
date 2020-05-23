namespace Ocelot.Multiplexer
{
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IResponseAggregator
    {
        Task Aggregate(ReRoute reRoute, HttpContext originalContext, List<HttpContext> downstreamResponses);
    }
}
