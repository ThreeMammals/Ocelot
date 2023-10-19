using System.Net.Http.Headers;

namespace Ocelot.Request.Middleware
{
    public class DownstreamRequest
    {
        private readonly HttpRequestMessage _request;

        public DownstreamRequest() { }

        public DownstreamRequest(HttpRequestMessage request)
        {
            _request = request;
            Method = _request.Method.Method;
            OriginalString = _request.RequestUri.OriginalString;
            Scheme = _request.RequestUri.Scheme;
            Host = _request.RequestUri.Host;
            Port = _request.RequestUri.Port;
            Headers = _request.Headers;
            AbsolutePath = _request.RequestUri.AbsolutePath;
            Query = _request.RequestUri.Query;
        }

        public virtual HttpHeaders Headers { get; }

        public virtual string Method { get; }

        public virtual string OriginalString { get; }

        public string Scheme { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string AbsolutePath { get; set; }

        public string Query { get; set; }

        public virtual bool HasContent { get => _request?.Content != null; }

        public virtual Task<string> ReadContentAsync() => HasContent
            ? _request.Content.ReadAsStringAsync()
            : Task.FromResult(string.Empty);

        public HttpRequestMessage ToHttpRequestMessage()
        {
            var uriBuilder = new UriBuilder
            {
                Port = Port,
                Host = Host,
                Path = AbsolutePath,
                Query = RemoveLeadingQuestionMark(Query),
                Scheme = Scheme,
            };

            _request.RequestUri = uriBuilder.Uri;
            _request.Method = new HttpMethod(Method);
            return _request;
        }

        public string ToUri()
        {
            var uriBuilder = new UriBuilder
            {
                Port = Port,
                Host = Host,
                Path = AbsolutePath,
                Query = RemoveLeadingQuestionMark(Query),
                Scheme = Scheme,
            };

            return uriBuilder.Uri.AbsoluteUri;
        }

        public override string ToString()
        {
            return ToUri();
        }

        private static string RemoveLeadingQuestionMark(string query)
        {
            if (!string.IsNullOrEmpty(query) && query.StartsWith('?'))
            {
                return query.Substring(1);
            }

            return query;
        }
    }
}
