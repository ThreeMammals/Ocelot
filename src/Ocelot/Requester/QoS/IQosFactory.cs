namespace Ocelot.Requester.QoS
{
    using System.Net.Http;

    using Configuration;

    using Responses;

    public interface IQoSFactory
    {
        Response<DelegatingHandler> Get(DownstreamRoute request);
    }
}
