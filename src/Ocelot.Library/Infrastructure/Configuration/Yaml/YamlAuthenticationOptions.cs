using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public class YamlAuthenticationOptions
    {
        public string Provider { get; set; }
        public string ProviderRootUrl { get; set; }
        public string ScopeName { get; set; }
        public bool RequireHttps { get; set; }
        public List<string> AdditionalScopes { get; set; }
        public string ScopeSecret { get; set; }
    }
}
