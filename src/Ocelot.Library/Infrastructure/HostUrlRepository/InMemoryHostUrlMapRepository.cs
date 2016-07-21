using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.HostUrlRepository
{
    public class InMemoryHostUrlMapRepository : IHostUrlMapRepository
    { 
        private readonly Dictionary<string, string> _routes;
        public InMemoryHostUrlMapRepository()
        {
            _routes = new Dictionary<string,string>();
        }
        public Response AddBaseUrlMap(HostUrlMap baseUrlMap)
        {
            if(_routes.ContainsKey(baseUrlMap.UrlPathTemplate)) 
            {
                return new ErrorResponse(new List<Error>(){new HostUrlMapKeyAlreadyExists()});
            }

            _routes.Add(baseUrlMap.UrlPathTemplate, baseUrlMap.UpstreamHostUrl);

            return new OkResponse();
        }

        public Response<HostUrlMap> GetBaseUrlMap(string urlPathTemplate)
        {
            string upstreamUrl = null;

            if(_routes.TryGetValue(urlPathTemplate, out upstreamUrl))
            {
                return new OkResponse<HostUrlMap>(new HostUrlMap(urlPathTemplate, upstreamUrl));
            }
    
            return new ErrorResponse<HostUrlMap>(new List<Error>(){new HostUrlMapKeyDoesNotExist()});
        } 
    } 
}