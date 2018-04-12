using System.Collections.Generic;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IRemoveOutputHeaders
    {
        Response Remove(List<Header> headers);
    }
}
