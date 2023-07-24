using Ocelot.Configuration.Creator;
using Ocelot.Middleware;
using System.Collections.Generic;

namespace Ocelot.Headers
{
    public interface IAddHeadersToResponse
    {
        void Add(List<AddHeader> addHeaders, DownstreamResponse response);
    }
}
