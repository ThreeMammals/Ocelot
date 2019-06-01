using Ocelot.Middleware;

namespace Ocelot.Requester
{
    public interface IHttpClientBuilder
    {
        IHttpClient Create(DownstreamContext request);

        void Save();
    }
}
