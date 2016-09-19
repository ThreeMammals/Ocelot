using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.AspNetCore.Http;

namespace Ocelot.Library.Infrastructure.Requester
{
    public interface IHttpRequester
    {
        Task<HttpResponseMessage> GetResponse(
            string httpMethod, 
            string downstreamUrl, 
            Stream content, 
            IHeaderDictionary headers, 
            IRequestCookieCollection cookies,
            IQueryCollection queryString,
            string contentType);
    }
}
