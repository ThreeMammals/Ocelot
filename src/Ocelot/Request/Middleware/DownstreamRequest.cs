namespace Ocelot.Request.Middleware
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public class DownstreamRequest
    {
        public DownstreamRequest(HttpRequestMessage request)
        {
            UriBuilder = new UriBuilder(request.RequestUri);
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

        //todo - can this not be get set
        public UriBuilder UriBuilder {get;set;}

        //todo - this gets called too much
        public HttpRequestMessage ToHttpRequestMessage()
        {
            _request.RequestUri = UriBuilder.Uri;
            return _request;
        }
    }
}