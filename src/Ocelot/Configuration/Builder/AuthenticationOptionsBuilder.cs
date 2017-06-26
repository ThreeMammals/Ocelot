using System.Collections.Generic;

namespace Ocelot.Configuration.Builder
{
    public class AuthenticationOptionsBuilder
    {

        private string _provider;

        private List<string> _allowedScopes;

        private IdentityServerConfig _identityServerConfig;

        public AuthenticationOptionsBuilder WithProvider(string provider)
        {
            _provider = provider;
            return this;
        }

        public AuthenticationOptionsBuilder WithAllowedScopes(List<string> allowedScopes)
        {
            _allowedScopes = allowedScopes;
            return this;
        }

        public AuthenticationOptionsBuilder WithIdntityServerConfigConfiguration(IdentityServerConfig config)
        {
            _identityServerConfig = config;
            return this;
        }

        public AuthenticationOptions Build()
        {
            return new AuthenticationOptions(_provider, _allowedScopes, _identityServerConfig);
        }
    }

    public class IdentityServerConfigBuilder
    {
        private string _providerRootUrl;
        private string _apiName;
        private string _apiSecret;
        private bool _requireHttps;
        
        public IdentityServerConfigBuilder WithProviderRootUrl(string providerRootUrl)
        {
            _providerRootUrl = providerRootUrl;
            return this;
        }

        public IdentityServerConfigBuilder WithApiName(string apiName)
        {
            _apiName = apiName;
            return this;
        }

        public IdentityServerConfigBuilder WithApiSecret(string apiSecret)
        {
            _apiSecret = apiSecret;
            return this;
        }

        public IdentityServerConfigBuilder WithRequireHttps(bool requireHttps)
        {
            _requireHttps = requireHttps;
            return this;
        }

       

        public IdentityServerConfig Build()
        {
            return new IdentityServerConfig(_providerRootUrl, _apiName, _requireHttps, _apiSecret);
        }
    }
}