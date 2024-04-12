namespace Ocelot.Configuration.Builder
{
    public class AuthenticationOptionsBuilder
    {
        private List<string> _allowedScopes = new();
        private string _authenticationProviderKey;
        private string[] _authenticationProviderKeys = Array.Empty<string>();

        public AuthenticationOptionsBuilder WithAllowedScopes(List<string> allowedScopes)
        {
            _allowedScopes = allowedScopes;
            return this;
        }

        [Obsolete("Use the " + nameof(WithAuthenticationProviderKeys) + " property!")]
        public AuthenticationOptionsBuilder WithAuthenticationProviderKey(string authenticationProviderKey)
        {
            _authenticationProviderKey = authenticationProviderKey;
            return this;
        }

        public AuthenticationOptionsBuilder WithAuthenticationProviderKeys(string[] authenticationProviderKeys)
        {
            _authenticationProviderKeys = authenticationProviderKeys;
            return this;
        }

        public AuthenticationOptions Build()
        {
            return new AuthenticationOptions(_allowedScopes, _authenticationProviderKey, _authenticationProviderKeys);
        }
    }
}
