using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public class HttpClientHttpRequester : IHttpRequester
    {
        public async Task<Response<HttpResponseMessage>> GetResponse(Request.Request request)
        {
            using (var handler = new HttpClientHandler { CookieContainer = request.CookieContainer })
            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    var response = await httpClient.SendAsync(request.HttpRequestMessage);
                    return new OkResponse<HttpResponseMessage>(response);
                }
                catch (Exception exception)
                {
                    return
                        new ErrorResponse<HttpResponseMessage>(new List<Error>
                        {
                            new UnableToCompleteRequestError(exception)
                        });
                }
            }
        }
    }
}