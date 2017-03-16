using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class AuthenticationOptions
    {
        public AuthenticationOptions(string provider, string providerRootUrl, string apiName, bool requireHttps, List<string> allowedScopes, string apiSecret)
        {
            Provider = provider;
            ProviderRootUrl = providerRootUrl;
			ApiName = apiName;
            RequireHttps = requireHttps;
			AllowedScopes = allowedScopes;
            ApiSecret = apiSecret;
        }

        public string Provider { get; private set; }
        public string ProviderRootUrl { get; private set; }
        public string ApiName { get; private set; }
        public string ApiSecret { get; private set; }
        public bool RequireHttps { get; private set; }
        public List<string> AllowedScopes { get; private set; }

    }
}
