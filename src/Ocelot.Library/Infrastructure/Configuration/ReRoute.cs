namespace Ocelot.Library.Infrastructure.Configuration
{
    public class ReRoute
    {
        public ReRoute(string downstreamTemplate, string upstreamTemplate, string upstreamHttpMethod, string upstreamTemplatePattern, bool isAuthenticated, string authenticationProvider)
        {
            DownstreamTemplate = downstreamTemplate;
            UpstreamTemplate = upstreamTemplate;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            IsAuthenticated = isAuthenticated;
            AuthenticationProvider = authenticationProvider;
        }

        public string DownstreamTemplate { get; private set; }
        public string UpstreamTemplate { get; private set; }
        public string UpstreamTemplatePattern { get; private set; }
        public string UpstreamHttpMethod { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public string AuthenticationProvider { get; private set; }
    }
}