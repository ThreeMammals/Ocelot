namespace Ocelot.Library.Infrastructure.RequestBuilder
{
    using System.IO;
    using System.Net.Http;
    using Microsoft.AspNetCore.Http;

    public interface IRequestBuilder
    {
        HttpRequestMessage Build(string httpMethod,
            string downstreamUrl,
            Stream content,
            IHeaderDictionary headers,
            IRequestCookieCollection cookies,
            IQueryCollection queryString,
            string contentType);
    }
}
