using System.Collections.Generic;
using System.Net;

namespace Ocelot.Cache
{
    public class CachedResponse
    {
        public CachedResponse(
            HttpStatusCode statusCode = HttpStatusCode.OK,
            Dictionary<string, IEnumerable<string>> headers = null,
            string body = null
            )
        {
            StatusCode = statusCode;
            Headers = headers ?? new Dictionary<string, IEnumerable<string>>();
            Body = body ?? "";
        }

        public HttpStatusCode StatusCode { get; private set; }

        public Dictionary<string, IEnumerable<string>> Headers { get; private set; }

        public string Body { get; private set; }
    }
}
