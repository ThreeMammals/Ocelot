namespace Ocelot.Library.RequestBuilder
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Responses;

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
