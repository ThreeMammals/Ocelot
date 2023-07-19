using Ocelot.Configuration;

namespace Ocelot.Requester
{
    public interface IHttpClientBuilder
    {
        IHttpClient Create(DownstreamRoute downstreamRoute);

        void Save();
    }
}
