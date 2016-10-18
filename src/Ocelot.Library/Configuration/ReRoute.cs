using System.Collections.Generic;
using Ocelot.Library.RequestBuilder;

namespace Ocelot.Library.Configuration
{
    public class ReRoute
    {
        public ReRoute(string downstreamTemplate, string upstreamTemplate, string upstreamHttpMethod, string upstreamTemplatePattern, bool isAuthenticated, AuthenticationOptions authenticationOptions, List<ConfigurationHeaderExtractorProperties> configurationHeaderExtractorProperties)
        {
            DownstreamTemplate = downstreamTemplate;
            UpstreamTemplate = upstreamTemplate;
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamTemplatePattern = upstreamTemplatePattern;
            IsAuthenticated = isAuthenticated;
            AuthenticationOptions = authenticationOptions;
            ConfigurationHeaderExtractorProperties = configurationHeaderExtractorProperties 
                ?? new List<ConfigurationHeaderExtractorProperties>();
        }

        public string DownstreamTemplate { get; private set; }
        public string UpstreamTemplate { get; private set; }
        public string UpstreamTemplatePattern { get; private set; }
        public string UpstreamHttpMethod { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public AuthenticationOptions AuthenticationOptions { get; private set; }
        public List<ConfigurationHeaderExtractorProperties> ConfigurationHeaderExtractorProperties { get; private set; } 
    }
}