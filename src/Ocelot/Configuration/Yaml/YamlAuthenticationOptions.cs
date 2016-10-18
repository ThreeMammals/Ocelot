namespace Ocelot.Library.Configuration.Yaml
{
    using System.Collections.Generic;

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
