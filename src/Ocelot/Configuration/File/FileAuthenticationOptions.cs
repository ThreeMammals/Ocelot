using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new List<string>();
            IdentityServerConfig = new FileIdentityServerConfig();
            JwtConfig = new FileJwtConfig();
        }

        public string Provider { get; set; }
        public List<string> AllowedScopes { get; set; }
        public FileIdentityServerConfig IdentityServerConfig { get; set; }
        public FileJwtConfig JwtConfig { get; set; }
    }

    public class FileIdentityServerConfig
    {
        public string ProviderRootUrl { get; set; }
        public string ApiName { get; set; }
        public bool RequireHttps { get; set; }
        public string ApiSecret { get; set; }
    }

    public class FileJwtConfig
    {
        public string Authority { get; set; }

        public string Audience { get; set; }
    }
}
