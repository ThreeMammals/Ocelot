namespace Ocelot.Requester
{
    using Ocelot.Configuration;

    public interface IHttpClientBuilder
    {
        IHttpClient Create(DownstreamReRoute downstreamReRoute);

        void Save();
    }
}
