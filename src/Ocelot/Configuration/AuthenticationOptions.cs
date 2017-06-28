using System.Collections.Generic;

namespace Ocelot.Configuration
{
    using Newtonsoft.Json;

    public class AuthenticationOptions
    {
        public AuthenticationOptions(string provider, List<string> allowedScopes, IAuthenticationConfig config)
        {
            Provider = provider;
            AllowedScopes = allowedScopes;
            Config = config;
        }

        public string Provider { get; private set; }
        
        public List<string> AllowedScopes { get; private set; }

        public IAuthenticationConfig Config { get; private set; }
    }

    public class IdentityServerConfig : IAuthenticationConfig
    {
        public IdentityServerConfig(string providerRootUrl, string apiName, bool requireHttps, string apiSecret)
        {
            ProviderRootUrl = providerRootUrl;
            ApiName = apiName;
            RequireHttps = requireHttps;
            ApiSecret = apiSecret;
        }

        public string ProviderRootUrl { get; private set; }
        public string ApiName { get; private set; }
        public string ApiSecret { get; private set; }
        public bool RequireHttps { get; private set; }
    }

    public class JwtConfig : IAuthenticationConfig
    {
        public JwtConfig(string authority, string audience)
        {
            Audience = audience;
            Authority = authority;
        }

        public string Audience { get; }

        public string Authority { get; }
    }

    public interface IAuthenticationConfig {}
}
