namespace Ocelot.Requester
{
    using Ocelot.Middleware;

    public interface IHttpClientBuilder
    {
        IHttpClient Create(IDownstreamContext request);

        void Save();
    }
}
