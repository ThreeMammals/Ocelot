using Ocelot.Configuration.Creator;
using Ocelot.Middleware;

namespace Ocelot.Headers
{
    public interface IAddHeadersToResponse
    {
        void Add(List<AddHeader> addHeaders, DownstreamResponse response);
    }
}
