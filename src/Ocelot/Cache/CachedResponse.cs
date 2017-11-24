using System.Collections.Generic;
using System.Net;

namespace Ocelot.Cache
{
    public class CachedResponse
    {
        public CachedResponse()
        {
            StatusCode = HttpStatusCode.OK;
            Headers = new Dictionary<string, IEnumerable<string>>();
            Body = "";
        }

        public HttpStatusCode StatusCode { get; set; }

        public Dictionary<string, IEnumerable<string>> Headers { get; set; }

        public string Body { get; set; }
    }
}
