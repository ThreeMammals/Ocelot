using System.Net.Http.Headers;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IRemoveHeaders
    {
        Response Remove(HttpResponseHeaders headers);
    }
}
