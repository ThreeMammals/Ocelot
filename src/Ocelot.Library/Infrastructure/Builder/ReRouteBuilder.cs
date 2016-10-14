namespace Ocelot.Library.Infrastructure.Builder
{
    using Configuration;

    public class ReRouteBuilder
    {
        private string _downstreamTemplate;
        private string _upstreamTemplate;
        private string _upstreamTemplatePattern;
        private string _upstreamHttpMethod;
        private bool _isAuthenticated;
        private string _authenticationProvider;

        public void WithDownstreamTemplate(string input)
        {
            _downstreamTemplate = input;
        }

        public void WithUpstreamTemplate(string input)
        {
            _upstreamTemplate = input;
        }

        public void WithUpstreamTemplatePattern(string input)
        {
            _upstreamTemplatePattern = input;
        }
        public void WithUpstreamHttpMethod(string input)
        {
            _upstreamHttpMethod = input;
        }
        public void WithIsAuthenticated(bool input)
        {
            _isAuthenticated = input;

        }
        public void WithAuthenticationProvider(string input)
        {
            _authenticationProvider = input;
        }

        public ReRoute Build()
        {
            return new ReRoute(_downstreamTemplate, _upstreamTemplate, _upstreamHttpMethod, _upstreamTemplatePattern, _isAuthenticated, _authenticationProvider);
        }
    }
}
