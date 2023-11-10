namespace Ocelot.Requester
{
    public interface IHttpClient
    {
        HttpClient Client { get; }

        DelegatingHandler ClientMainHandler { get; }

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    }
}
