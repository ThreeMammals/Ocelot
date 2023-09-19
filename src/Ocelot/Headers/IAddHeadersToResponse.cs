using Ocelot.Middleware;
using System.Collections.Generic;

using Ocelot.Configuration.Creator;

namespace Ocelot.Headers
{
    public interface IAddHeadersToResponse
    {
        void Add(List<AddHeader> addHeaders, DownstreamResponse response);
    }
}
