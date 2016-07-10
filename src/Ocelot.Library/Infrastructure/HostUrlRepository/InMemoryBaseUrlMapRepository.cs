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
            if(_routes.ContainsKey(baseUrlMap.DownstreamHostUrl)) 
            {
                return new ErrorResponse(new List<Error>(){new HostUrlMapKeyAlreadyExists()});
            }

            _routes.Add(baseUrlMap.DownstreamHostUrl, baseUrlMap.UpstreamHostUrl);

            return new OkResponse();
        }

        public Response<HostUrlMap> GetBaseUrlMap(string downstreamUrl)
        {
            string upstreamUrl = null;

            if(_routes.TryGetValue(downstreamUrl, out upstreamUrl))
            {
                return new OkResponse<HostUrlMap>(new HostUrlMap(downstreamUrl, upstreamUrl));
            }
    
            return new ErrorResponse<HostUrlMap>(new List<Error>(){new HostUrlMapKeyDoesNotExist()});
        } 
    } 
}