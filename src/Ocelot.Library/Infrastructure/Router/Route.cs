namespace Ocelot.Library.Infrastructure.Router
{
    public class Route
    {
        public Route(string apiKey, string upstreamRoute)
        {
            ApiKey = apiKey;
            UpstreamRoute = upstreamRoute;
        }

        public string ApiKey {get;private set;}
        public string UpstreamRoute {get;private set;}
    }
}