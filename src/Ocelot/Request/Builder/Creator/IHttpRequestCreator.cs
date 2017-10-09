using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using System.Net.Http;

namespace Ocelot.Request.Builder
{
    public interface IHttpRequestCreator
    {
        Task<Response<Request>> Build(HttpRequestMessage httpRequestMessage,
            bool isQos,
            IQoSProvider qosProvider);
    }
}
