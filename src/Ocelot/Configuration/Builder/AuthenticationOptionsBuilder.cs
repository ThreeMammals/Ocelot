using System.Collections.Generic;

namespace Ocelot.Configuration.Builder
{
    public class AuthenticationOptionsBuilder
    {
        private List<string> _allowedScopes = new List<string>();
        private string _authenticationProviderKey;

        public AuthenticationOptionsBuilder WithAllowedScopes(List<string> allowedScopes)
        {
            _allowedScopes = allowedScopes;
            return this;
        }

        public AuthenticationOptionsBuilder WithAuthenticationProviderKey(string authenticationProviderKey)
        {
            _authenticationProviderKey = authenticationProviderKey;
            return this;
        }

        public AuthenticationOptions Build()
        {
            return new AuthenticationOptions(_allowedScopes, _authenticationProviderKey);
        }
    }
}