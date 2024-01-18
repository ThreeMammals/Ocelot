namespace Ocelot.Middleware
{
    public class DownstreamResponse : IDisposable
    {
        // To detect redundant calls
        private bool _disposedValue;
        private readonly HttpResponseMessage _responseMessage;

        public DownstreamResponse(HttpContent content, HttpStatusCode statusCode, List<Header> headers,
            string reasonPhrase)
        {
            Content = content;
            StatusCode = statusCode;
            Headers = headers ?? new();
            ReasonPhrase = reasonPhrase;
        }

        public DownstreamResponse(HttpResponseMessage response)
            : this(response.Content, response.StatusCode,
                response.Headers.Select(x => new Header(x.Key, x.Value)).ToList(), response.ReasonPhrase)
        {
            _responseMessage = response;
        }

        public DownstreamResponse(HttpContent content, HttpStatusCode statusCode,
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, string reasonPhrase)
            : this(content, statusCode, headers.Select(x => new Header(x.Key, x.Value)).ToList(), reasonPhrase)
        {
        }

        public HttpContent Content { get; }
        public HttpStatusCode StatusCode { get; }
        public List<Header> Headers { get; }
        public string ReasonPhrase { get; }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// We should make sure we dispose the content and response message to close the connection to the downstream service.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                Content?.Dispose();
                _responseMessage?.Dispose();
            }

            _disposedValue = true;
        }
    }
}
