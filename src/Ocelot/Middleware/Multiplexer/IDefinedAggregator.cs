using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IDefinedAggregator
    {
        Task<DownstreamResponse> Aggregate(List<DownstreamContext> responses);
    }
}
