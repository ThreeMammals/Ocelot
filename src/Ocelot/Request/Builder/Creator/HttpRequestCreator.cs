using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Responses;
using Ocelot.Configuration;
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
            return await Task.FromResult(new OkResponse<Request>(new HttpRequest(httpRequestMessage, isQos, qosProvider)));
        }
    }
}