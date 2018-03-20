namespace Ocelot.Request.Middleware
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public class DownstreamRequest
    {
        public DownstreamRequest(HttpRequestMessage request)
        {
            _request = request;
        }
        private HttpRequestMessage _request;
        public string Method => _request.Method.Method;
        public string OriginalString => _request.RequestUri.OriginalString;
        public string Scheme => _request.RequestUri.Scheme;
        public string Host => _request.RequestUri.Host;
        public int Port => _request.RequestUri.Port;
        public HttpRequestHeaders Headers => _request.Headers;
        public string Authority => _request.RequestUri.Authority;
        public string AbsolutePath => _request.RequestUri.AbsolutePath;

        public Uri RequestUri => _request.RequestUri;

        public HttpRequestMessage ToHttpRequestMessage()
        {
            return _request;
        }
    }
}