using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new List<string>();
        }

        public string AuthenticationProviderKey {get; set;}
        public List<string> AllowedScopes { get; set; }
    }
}
