using System;

namespace Ocelot.Configuration.Builder
{
    public class AuthenticationOptionsBuilder
    {
        private List<string> _allowedScopes = new();
        private List<string> _requiredRole = new();
        private string[] _authenticationProviderKeys = Array.Empty<string>();
        private string _roleKey;
        private string _scopeKey;
        private string _policyName;

        public AuthenticationOptionsBuilder WithAllowedScopes(List<string> allowedScopes)
        {
            _allowedScopes = allowedScopes;
            return this;
        }

        public AuthenticationOptionsBuilder WithRequiredRole(List<string> requiredRole)
        {
            _requiredRole = requiredRole;
            return this;
        }

        [Obsolete("Use the " + nameof(WithAuthenticationProviderKeys) + " property!")]
        public AuthenticationOptionsBuilder WithAuthenticationProviderKey(string authenticationProviderKey)
            => WithAuthenticationProviderKeys([authenticationProviderKey]);

        public AuthenticationOptionsBuilder WithAuthenticationProviderKeys(string[] authenticationProviderKeys)
        {
            _authenticationProviderKeys = authenticationProviderKeys;
            return this;
        }

        public AuthenticationOptionsBuilder WithRoleKey(string roleKey)
        {
            _roleKey = roleKey;
            return this;
        }

        public AuthenticationOptionsBuilder WithScopeKey(string scopeKey)
        {
            _scopeKey = scopeKey;
            return this;
        }

        public AuthenticationOptionsBuilder WithPolicyName(string policyName)
        {
            _policyName = policyName;
            return this;
        }

        public AuthenticationOptions Build()
        {
            return new AuthenticationOptions(_allowedScopes, _authenticationProviderKeys, _requiredRole, _scopeKey, _roleKey, _policyName);
        }
    }
}
