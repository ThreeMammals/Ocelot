namespace Ocelot.Requester
{
    using Configuration;

    public interface IHttpClientBuilder
    {
        IHttpClient Create(DownstreamRoute downstreamRoute);

        void Save();
    }
}
