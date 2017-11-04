namespace Ocelot.Request.Builder
{
    using System.Net.Http;
    using System.Threading.Tasks;

    using Ocelot.Requester.QoS;
    using Ocelot.Responses;

    public interface IRequestCreator
    {
        Task<Response<Request>> Build(
            HttpRequestMessage httpRequestMessage,
            bool isQos,
            IQoSProvider qosProvider,
            bool useCookieContainer,
            bool allowAutoRedirect);
    }
}
