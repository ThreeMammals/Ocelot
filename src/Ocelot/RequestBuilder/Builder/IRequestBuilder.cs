using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Ocelot.Library.Responses;

namespace Ocelot.Library.RequestBuilder.Builder
{
    public interface IRequestBuilder
    {
        Task<Response<Request>> Build(string httpMethod,
            string downstreamUrl,
            Stream content,
            IHeaderDictionary headers,
            IRequestCookieCollection cookies,
            string queryString,
            string contentType);
    }
}
