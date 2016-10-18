using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class AuthenticationOptions
    {
        public AuthenticationOptions(string provider, string providerRootUrl, string scopeName, bool requireHttps, List<string> additionalScopes, string scopeSecret)
        {
            Provider = provider;
            ProviderRootUrl = providerRootUrl;
            ScopeName = scopeName;
            RequireHttps = requireHttps;
            AdditionalScopes = additionalScopes;
            ScopeSecret = scopeSecret;
        }

        public string Provider { get; private set; }
        public string ProviderRootUrl { get; private set; }
        public string ScopeName { get; private set; }
        public string ScopeSecret { get; private set; }
        public bool RequireHttps { get; private set; }
        public List<string> AdditionalScopes { get; private set; }

    }
}
