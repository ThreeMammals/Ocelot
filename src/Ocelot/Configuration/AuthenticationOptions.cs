using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public sealed class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey)
        {
            AllowedScopes = allowedScopes;
            AuthenticationProviderKey = authenticationProviderKey;
        }

        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey, List<string> authenticationProviderKeys)
            : this(allowedScopes, authenticationProviderKey)
        {
            AuthenticationProviderKeys = authenticationProviderKeys;
        }

        public List<string> AllowedScopes { get; }

        public string AuthenticationProviderKey { get; }

        public List<string> AuthenticationProviderKeys { get; }
    }
}
