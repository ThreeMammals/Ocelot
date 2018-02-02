using System.Net.Http.Headers;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IRemoveOutputHeaders
    {
        Response Remove(HttpResponseHeaders headers);
    }
}
