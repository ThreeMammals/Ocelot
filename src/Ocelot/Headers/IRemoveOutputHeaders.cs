using Ocelot.Middleware;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IRemoveOutputHeaders
    {
        Response Remove(List<Header> headers);
    }
}
