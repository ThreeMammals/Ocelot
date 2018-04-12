using Ocelot.Middleware;

namespace Ocelot.Headers
{
    using System.Collections.Generic;
    using Ocelot.Configuration.Creator;
    using Ocelot.Middleware.Multiplexer;

    public interface IAddHeadersToResponse
    {
        void Add(List<AddHeader> addHeaders, DownstreamResponse response);
    }
}
