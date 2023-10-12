namespace Ocelot.Configuration
{
    public class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey)
        {
            AllowedScopes = allowedScopes;
            AuthenticationProviderKey = authenticationProviderKey;
        }

        public List<string> AllowedScopes { get; }
        public string AuthenticationProviderKey { get; }
    }
}
