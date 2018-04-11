using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Ocelot.Middleware.Multiplexer
{
    public class AggregateResponse
    {
        public AggregateResponse(HttpContent content, HttpStatusCode statusCode, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            Content = content;
            StatusCode = statusCode;
            Headers = headers;
        }

        public HttpContent Content { get; }
        public HttpStatusCode StatusCode { get; }
        public List<KeyValuePair<string, IEnumerable<string>>> Headers { get; }
    }
}
