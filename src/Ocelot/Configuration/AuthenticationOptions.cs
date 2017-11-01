using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes)
        {
            AllowedScopes = allowedScopes;
        }

        public List<string> AllowedScopes { get; private set; }
    }
}
