using Ocelot.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IResponseAggregator
    {
        Task Aggregate(ReRoute reRoute, DownstreamContext originalContext, List<DownstreamContext> downstreamResponses);
    }
}
