using System;
using System.Collections.Generic;
using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Router
{
    public class InMemoryRouterService : IRouterService
    {
        private readonly Dictionary<string, string> _routes;
        public InMemoryRouterService()
        {
            _routes = new Dictionary<string,string>();
        }
        public Response AddRoute(string apiKey, string upstreamApiBaseUrl)
        {
            if(_routes.ContainsKey(apiKey))
            {
                return new ErrorResponse(new List<Error>(){new RouteKeyAlreadyExists("This key has already been used")});
            }

            _routes.Add(apiKey, upstreamApiBaseUrl);

            return new OkResponse();
        }

        public Response<Route> GetRoute(string apiKey)
        {
            Console.WriteLine("looking for {0}", apiKey);
            string upstreamApiBaseUrl = null;

            if(_routes.TryGetValue(apiKey, out upstreamApiBaseUrl))
            {
                return new OkResponse<Route>(new Route(apiKey, upstreamApiBaseUrl));
            }

            Console.WriteLine("Couldnt find it");
    
            return new ErrorResponse<Route>(new List<Error>(){new RouteKeyDoesNotExist("This key does not exist")});
        } 
    } 
}