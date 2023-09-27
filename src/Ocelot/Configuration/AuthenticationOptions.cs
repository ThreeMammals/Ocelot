namespace Ocelot.Configuration
{
    public sealed class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey)
        {
            AllowedScopes = allowedScopes;
            AuthenticationProviderKey = authenticationProviderKey;
        }

        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey, string[] authenticationProviderKeys)
            : this(allowedScopes, authenticationProviderKey)
        {
            AuthenticationProviderKeys = authenticationProviderKeys;
        }

        public List<string> AllowedScopes { get; }

        public string AuthenticationProviderKey { get; }

        public string[] AuthenticationProviderKeys { get; }
    }
}
