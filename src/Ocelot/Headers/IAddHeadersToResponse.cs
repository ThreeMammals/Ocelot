using Ocelot.Middleware;

namespace Ocelot.Headers
{
    using System.Collections.Generic;

    using Configuration.Creator;

    public interface IAddHeadersToResponse
    {
        void Add(List<AddHeader> addHeaders, DownstreamResponse response);
    }
}
