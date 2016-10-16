using System.Collections.Generic;

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
        private string _authenticationProviderUrl;
        private string _scopeName;
        private List<string> _additionalScopes;
        private bool _requireHttps;
        private string _scopeSecret;

        public ReRouteBuilder()
        {
            _additionalScopes = new List<string>();
        }

        public ReRouteBuilder WithDownstreamTemplate(string input)
        {
            _downstreamTemplate = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamTemplate(string input)
        {
            _upstreamTemplate = input;
            return this;
        }

        public ReRouteBuilder WithUpstreamTemplatePattern(string input)
        {
            _upstreamTemplatePattern = input;
            return this;
        }
        public ReRouteBuilder WithUpstreamHttpMethod(string input)
        {
            _upstreamHttpMethod = input;
            return this;
        }
        public ReRouteBuilder WithIsAuthenticated(bool input)
        {
            _isAuthenticated = input;
            return this;

        }
        public ReRouteBuilder WithAuthenticationProvider(string input)
        {
            _authenticationProvider = input;
            return this;
        }

        public ReRouteBuilder WithAuthenticationProviderUrl(string input)
        {
            _authenticationProviderUrl = input;
            return this;
        }

        public ReRouteBuilder WithAuthenticationProviderScopeName(string input)
        {
            _scopeName = input;
            return this;
        }

        public ReRouteBuilder WithAuthenticationProviderAdditionalScopes(List<string> input)
        {
            _additionalScopes = input;
            return this;
        }

        public ReRouteBuilder WithRequireHttps(bool input)
        {
            _requireHttps = input;
            return this;
        }

        public ReRouteBuilder WithScopeSecret(string input)
        {
            _scopeSecret = input;
            return this;
        }

        public ReRoute Build()
        {
            return new ReRoute(_downstreamTemplate, _upstreamTemplate, _upstreamHttpMethod, _upstreamTemplatePattern, _isAuthenticated, new AuthenticationOptions(_authenticationProvider, _authenticationProviderUrl, _scopeName, _requireHttps, _additionalScopes, _scopeSecret));
        }
    }
}
