namespace Ocelot.Library.Infrastructure.Router.UpstreamRouter
{
    public class Route
    {
        public Route(string downstreamUrl, string upstreamUrl)
        {
            DownstreamUrl = downstreamUrl;
            UpstreamUrl = upstreamUrl;
        }

        public string DownstreamUrl {get;private set;}
        public string UpstreamUrl {get;private set;}
    }
}