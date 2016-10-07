using System.IO;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Library.Infrastructure.RequestBuilder
{
    public interface IRequestBuilder
    {
        Request Build(string httpMethod,
            string downstreamUrl,
            Stream content,
            IHeaderDictionary headers,
            IRequestCookieCollection cookies,
            string queryString,
            string contentType);
    }
}
