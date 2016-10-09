namespace Ocelot.Library.Infrastructure.Configuration
{
    public class ReRoute
    {
        public ReRoute(string downstreamTemplate, string upstreamTemplate, string upstreamHttpMethod, string upstreamTemplatePattern)
        {
            DownstreamTemplate = downstreamTemplate;
            UpstreamTemplate = upstreamTemplate;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
        }

        public string DownstreamTemplate { get; private set; }
        public string UpstreamTemplate { get; private set; }
        public string UpstreamTemplatePattern { get; private set; }
        public string UpstreamHttpMethod { get; private set; }
    }
}