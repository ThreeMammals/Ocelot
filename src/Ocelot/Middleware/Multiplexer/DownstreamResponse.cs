using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Ocelot.Middleware.Multiplexer
{
    public class DownstreamResponse
    {
        public DownstreamResponse(HttpResponseMessage response)
        {
            Content = response.Content;
            StatusCode = response.StatusCode;
            Headers = response.Headers.ToList();
        }

        public DownstreamResponse(HttpContent content, HttpStatusCode statusCode, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            Content = content;
            StatusCode = statusCode;
            Headers = headers ?? new List<KeyValuePair<string, IEnumerable<string>>>();
        }

        public HttpContent Content { get; }
        public HttpStatusCode StatusCode { get; }
        public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; }
    }
}
