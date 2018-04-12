using System.Collections.Generic;
using System.Net.Http.Headers;
using Ocelot.Responses;

namespace Ocelot.Headers
{
    public interface IRemoveOutputHeaders
    {
        Response Remove(List<KeyValuePair<string, IEnumerable<string>>> headers);
    }
}
