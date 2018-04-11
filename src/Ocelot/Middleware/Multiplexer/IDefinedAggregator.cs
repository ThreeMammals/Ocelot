using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IDefinedAggregator
    {
        Task<HttpResponseMessage> Aggregate(List<HttpResponseMessage> responses);
    }
}
