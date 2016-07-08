using System;
using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router.UrlPathRouter
{
    public class InMemoryUrlPathRouter : IUrlPathRouter
    { 
        private readonly Dictionary<string, string> _routes;
        public InMemoryUrlPathRouter()
        {
            _routes = new Dictionary<string,string>();
        }
        public Response AddRoute(string downstreamUrlPathTemplate, string upstreamUrlPathTemplate)
        {
            if(_routes.ContainsKey(downstreamUrlPathTemplate))
            {
                return new ErrorResponse(new List<Error>(){new DownstreamUrlPathTemplateAlreadyExists()});
            }

            _routes.Add(downstreamUrlPathTemplate, upstreamUrlPathTemplate);

            return new OkResponse();
        }

        public Response<UrlPath> GetRoute(string downstreamUrlPathTemplate)
        {
            string upstreamUrlPathTemplate = null;

            if(_routes.TryGetValue(downstreamUrlPathTemplate, out upstreamUrlPathTemplate))
            {
                return new OkResponse<UrlPath>(new UrlPath(downstreamUrlPathTemplate, upstreamUrlPathTemplate));
            }

            return new ErrorResponse<UrlPath>(new List<Error>(){new DownstreamUrlPathTemplateDoesNotExist()});
        } 
    } 
}