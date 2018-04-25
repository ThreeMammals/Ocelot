using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Ocelot.Middleware
{
    public class DownstreamResponse
    {
        public DownstreamResponse(HttpContent content, HttpStatusCode statusCode, List<Header> headers)
        {
            Content = content;
            StatusCode = statusCode;
            Headers = headers ?? new List<Header>();
        }

        public DownstreamResponse(HttpResponseMessage response)
            :this(response.Content, response.StatusCode, response.Headers.Select(x => new Header(x.Key, x.Value)).ToList())
        {
        }

        public DownstreamResponse(HttpContent content, HttpStatusCode statusCode, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
            :this(content, statusCode, headers.Select(x => new Header(x.Key, x.Value)).ToList())
        {
        }

        public HttpContent Content { get; }
        public HttpStatusCode StatusCode { get; }
        public List<Header> Headers { get; }
    }
}
