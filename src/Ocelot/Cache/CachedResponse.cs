namespace Ocelot.Cache
{
    public class CachedResponse
    {
        public CachedResponse(
            HttpStatusCode statusCode,
            Dictionary<string, IEnumerable<string>> headers,
            string body,
            Dictionary<string, IEnumerable<string>> contentHeaders,
            string reasonPhrase
            )
        {
            StatusCode = statusCode;
            Headers = headers ?? new Dictionary<string, IEnumerable<string>>();
            ContentHeaders = contentHeaders ?? new Dictionary<string, IEnumerable<string>>();
            Body = body ?? string.Empty;
            ReasonPhrase = reasonPhrase;
        }

        public HttpStatusCode StatusCode { get; }

        public Dictionary<string, IEnumerable<string>> Headers { get; }

        public Dictionary<string, IEnumerable<string>> ContentHeaders { get; }

        public string Body { get; }

        public string ReasonPhrase { get; }
    }
}
