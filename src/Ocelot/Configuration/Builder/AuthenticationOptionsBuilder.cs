using System.Collections.Generic;

namespace Ocelot.Configuration.Builder
{
    public class AuthenticationOptionsBuilder
    {

        private string _provider;
        private string _providerRootUrl;
        private string _apiName;
        private string _apiSecret;
        private bool _requireHttps;
        private List<string> _allowedScopes;

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

        public AuthenticationOptionsBuilder WithApiName(string apiName)
        {
            _apiName = apiName;
            return this;
        }

        public AuthenticationOptionsBuilder WithApiSecret(string apiSecret)
        {
            _apiSecret = apiSecret;
            return this;
        }

        public AuthenticationOptionsBuilder WithRequireHttps(bool requireHttps)
        {
            _requireHttps = requireHttps;
            return this;
        }

        public AuthenticationOptionsBuilder WithAllowedScopes(List<string> allowedScopes)
        {
            _allowedScopes = allowedScopes;
            return this;
        }

        public AuthenticationOptions Build()
        {
            return new AuthenticationOptions(_provider, _providerRootUrl, _apiName, _requireHttps, _allowedScopes, _apiSecret);
        }
    }
}