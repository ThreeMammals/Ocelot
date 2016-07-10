using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.BaseUrlRepository
{
    public class InMemoryBaseUrlMapRepository : IBaseUrlMapRepository
    { 
        private readonly Dictionary<string, string> _routes;
        public InMemoryBaseUrlMapRepository()
        {
            _routes = new Dictionary<string,string>();
        }
        public Response AddBaseUrlMap(BaseUrlMap baseUrlMap)
        {
            if(_routes.ContainsKey(baseUrlMap.DownstreamBaseUrl)) 
            {
                return new ErrorResponse(new List<Error>(){new BaseUrlMapKeyAlreadyExists()});
            }

            _routes.Add(baseUrlMap.DownstreamBaseUrl, baseUrlMap.UpstreamBaseUrl);

            return new OkResponse();
        }

        public Response<BaseUrlMap> GetBaseUrlMap(string downstreamUrl)
        {
            string upstreamUrl = null;

            if(_routes.TryGetValue(downstreamUrl, out upstreamUrl))
            {
                return new OkResponse<BaseUrlMap>(new BaseUrlMap(downstreamUrl, upstreamUrl));
            }
    
            return new ErrorResponse<BaseUrlMap>(new List<Error>(){new BaseUrlMapKeyDoesNotExist()});
        } 
    } 
}