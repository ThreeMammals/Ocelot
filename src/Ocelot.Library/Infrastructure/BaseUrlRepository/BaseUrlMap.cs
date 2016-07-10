namespace Ocelot.Library.Infrastructure.BaseUrlRepository
{
    public class BaseUrlMap
    {
        public BaseUrlMap(string downstreamBaseUrl, string upstreamBaseUrl)
        {
            DownstreamBaseUrl = downstreamBaseUrl;
            UpstreamBaseUrl = upstreamBaseUrl;
        }

        public string DownstreamBaseUrl {get;private set;}
        public string UpstreamBaseUrl {get;private set;}
    }
}