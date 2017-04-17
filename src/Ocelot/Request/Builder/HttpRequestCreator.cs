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
        //public async Task<Response<Request>> Build(
        //    string httpMethod, 
        //    string downstreamUrl, 
        //    Stream content, 
        //    IHeaderDictionary headers,
        //    QueryString queryString, 
        //    string contentType, 
        //    RequestId.RequestId requestId,
        //    bool isQos,
        //    IQoSProvider qosProvider)
        //{
        //    var request = await new RequestBuilder()
        //        .WithHttpMethod(httpMethod)
        //        .WithDownstreamUrl(downstreamUrl)
        //        .WithQueryString(queryString)
        //        .WithContent(content)
        //        .WithContentType(contentType)
        //        .WithHeaders(headers)
        //        .WithRequestId(requestId)
        //        .WithIsQos(isQos)
        //        .WithQos(qosProvider)
        //        .Build();

        //    return new OkResponse<Request>(request);
        //}

        public async Task<Response<Request>> Build(
            HttpRequestMessage httpRequestMessage,
            bool isQos,
            IQoSProvider qosProvider)
        {
            return new OkResponse<Request>(new Request(httpRequestMessage, isQos, qosProvider));
        }
    }
}