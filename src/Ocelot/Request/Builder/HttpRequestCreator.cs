using System.Threading.Tasks;
using Ocelot.Responses;
using Ocelot.Requester.QoS;
using System.Net.Http;

namespace Ocelot.Request.Builder
{
    public sealed class HttpRequestCreator : IRequestCreator
    {
        public async Task<Response<Request>> Build(
            HttpRequestMessage httpRequestMessage,
            bool isQos,
            IQoSProvider qosProvider)
        {
            return new OkResponse<Request>(new Request(httpRequestMessage, isQos, qosProvider));
        }
    }
}