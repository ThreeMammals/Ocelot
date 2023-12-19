using Ocelot.Configuration.File;

namespace Ocelot.Configuration
{
    public sealed class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey)
        {
            AllowedScopes = allowedScopes;
            AuthenticationProviderKey = authenticationProviderKey;
            AuthenticationProviderKeys = Array.Empty<string>();
        }

        public AuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = from.AllowedScopes ?? new();
            AuthenticationProviderKey = from.AuthenticationProviderKey ?? string.Empty;
            AuthenticationProviderKeys = from.AuthenticationProviderKeys ?? Array.Empty<string>();
        }

        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey, string[] authenticationProviderKeys)
        {
            AllowedScopes = allowedScopes ?? new();
            AuthenticationProviderKey = authenticationProviderKey ?? string.Empty;
            AuthenticationProviderKeys = authenticationProviderKeys ?? Array.Empty<string>();
        }

        public List<string> AllowedScopes { get; }
        public string AuthenticationProviderKey { get; }
        public string[] AuthenticationProviderKeys { get; }
    }
}
