using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AdditionalScopes = new List<string>();
        }

        public string Provider { get; set; }
        public string ProviderRootUrl { get; set; }
        public string ScopeName { get; set; }
        public bool RequireHttps { get; set; }
        public List<string> AdditionalScopes { get; set; }
        public string ScopeSecret { get; set; }
    }
}
