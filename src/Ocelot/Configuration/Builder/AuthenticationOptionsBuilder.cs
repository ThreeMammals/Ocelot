using System.Collections.Generic;

namespace Ocelot.Configuration.Builder
{
    public class AuthenticationOptionsBuilder
    {

        private string _provider;
        private string _providerRootUrl;
        private string _scopeName;
        private string _scopeSecret;
        private bool _requireHttps;
        private List<string> _additionalScopes;

        public AuthenticationOptionsBuilder WithProvider(string provider)
        {
            _provider = provider;
            return this;
        }

        public AuthenticationOptionsBuilder WithProviderRootUrl(string providerRootUrl)
        {
            _providerRootUrl = providerRootUrl;
            return this;
        }

        public AuthenticationOptionsBuilder WithScopeName(string scopeName)
        {
            _scopeName = scopeName;
            return this;
        }

        public AuthenticationOptionsBuilder WithScopeSecret(string scopeSecret)
        {
            _scopeSecret = scopeSecret;
            return this;
        }

        public AuthenticationOptionsBuilder WithRequireHttps(bool requireHttps)
        {
            _requireHttps = requireHttps;
            return this;
        }

        public AuthenticationOptionsBuilder WithAdditionalScopes(List<string> additionalScopes)
        {
            _additionalScopes = additionalScopes;
            return this;
        }

        public AuthenticationOptions Build()
        {
            return new AuthenticationOptions(_provider, _providerRootUrl, _scopeName, _requireHttps, _additionalScopes, _scopeSecret);
        }
    }
}