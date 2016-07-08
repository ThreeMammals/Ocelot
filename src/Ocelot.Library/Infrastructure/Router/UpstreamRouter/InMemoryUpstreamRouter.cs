using System;
using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router.UpstreamRouter
{
    public class InMemoryUpstreamRouter : IUpstreamRouter
    { 
        private readonly Dictionary<string, string> _routes;
        public InMemoryUpstreamRouter()
        {
            _routes = new Dictionary<string,string>();
        }
        public Response AddRoute(string downstreamUrl, string upstreamUrl)
        {
            if(_routes.ContainsKey(downstreamUrl)) 
            {
                return new ErrorResponse(new List<Error>(){new RouteKeyAlreadyExists()});
            }

            _routes.Add(downstreamUrl, upstreamUrl);

            return new OkResponse();
        }

        public Response<Route> GetRoute(string downstreamUrl)
        {
            string upstreamUrl = null;

            if(_routes.TryGetValue(downstreamUrl, out upstreamUrl))
            {
                return new OkResponse<Route>(new Route(downstreamUrl, upstreamUrl));
            }
    
            return new ErrorResponse<Route>(new List<Error>(){new RouteKeyDoesNotExist()});
        } 
    } 
}