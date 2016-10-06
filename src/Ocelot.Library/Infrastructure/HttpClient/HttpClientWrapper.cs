namespace Ocelot.Library.Infrastructure.HttpClient
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public class HttpClientWrapper : IHttpClient
    {
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        public Uri BaseAddress
        {
            get { return _httpClient.BaseAddress; }
            set { _httpClient.BaseAddress = value; }
        }

        public HttpRequestHeaders DefaultRequestHeaders
        {
            get { return _httpClient.DefaultRequestHeaders; }
        }

        public long MaxResponseContentBufferSize
        {
            get { return _httpClient.MaxResponseContentBufferSize; }
            set { _httpClient.MaxResponseContentBufferSize = value; }
        }

        public TimeSpan Timeout
        {
            get { return _httpClient.Timeout; }
            set { _httpClient.Timeout = value; }
        }

        public void CancelPendingRequests()
        {
            _httpClient.CancelPendingRequests();
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
        {
            return await _httpClient.DeleteAsync(requestUri);
        }

        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri)
        {
            return await _httpClient.DeleteAsync(requestUri);
        }

        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return await _httpClient.DeleteAsync(requestUri, cancellationToken);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken)
        {
            return await _httpClient.DeleteAsync(requestUri, cancellationToken);
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return await _httpClient.GetAsync(requestUri);
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri)
        {
            return await _httpClient.GetAsync(requestUri);
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption)
        {
            return await _httpClient.GetAsync(requestUri, completionOption);
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken)
        {
            return await _httpClient.GetAsync(requestUri, cancellationToken);
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption)
        {
            return await _httpClient.GetAsync(requestUri, completionOption);
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return await _httpClient.GetAsync(requestUri, cancellationToken);
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return await _httpClient.GetAsync(requestUri, completionOption, cancellationToken);
        }

        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return await _httpClient.GetAsync(requestUri, completionOption, cancellationToken);
        }

        public async Task<byte[]> GetByteArrayAsync(string requestUri)
        {
            return await _httpClient.GetByteArrayAsync(requestUri);
        }

        public async Task<byte[]> GetByteArrayAsync(Uri requestUri)
        {
            return await _httpClient.GetByteArrayAsync(requestUri);
        }

        public async Task<Stream> GetStreamAsync(string requestUri)
        {
            return await _httpClient.GetStreamAsync(requestUri);
        }

        public async Task<Stream> GetStreamAsync(Uri requestUri)
        {
            return await _httpClient.GetStreamAsync(requestUri);
        }

        public async Task<string> GetStringAsync(string requestUri)
        {
            return await _httpClient.GetStringAsync(requestUri);
        }

        public async Task<string> GetStringAsync(Uri requestUri)
        {
            return await _httpClient.GetStringAsync(requestUri);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return await _httpClient.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content)
        {
            return await _httpClient.PostAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return await _httpClient.PostAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return await _httpClient.PostAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content)
        {
            return await _httpClient.PutAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content)
        {
            return await _httpClient.PutAsync(requestUri, content);
        }

        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return await _httpClient.PutAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return await _httpClient.PutAsync(requestUri, content, cancellationToken);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption)
        {
            return await _httpClient.SendAsync(request, completionOption);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return await _httpClient.SendAsync(request, completionOption, cancellationToken);
        }

        public void Dispose(bool disposing)
        {
            if (!disposing)
            {
                _httpClient.Dispose();
            }
        }
    }
}
