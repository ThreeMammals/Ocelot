namespace Ocelot.Library.Infrastructure.HostUrlRepository
{
    public class HostUrlMap
    {
        public HostUrlMap(string downstreamHostUrl, string upstreamHostUrl)
        {
            DownstreamHostUrl = downstreamHostUrl;
            UpstreamHostUrl = upstreamHostUrl;
        }

        public string DownstreamHostUrl {get;private set;}
        public string UpstreamHostUrl {get;private set;}
    }
}