using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, List<string> requiredRole, string authenticationProviderKey, string scopeKey, string roleKey, string policyName)
        {
            PolicyName = policyName;
            AllowedScopes = allowedScopes;
            RequiredRole = requiredRole;
            AuthenticationProviderKey = authenticationProviderKey;
            ScopeKey = scopeKey;
            RoleKey = roleKey;
        }

        public List<string> AllowedScopes { get; }
        public string AuthenticationProviderKey { get; }
        public List<string> RequiredRole { get; }
        public string ScopeKey { get; }
        public string RoleKey { get; }
        public string PolicyName { get; }
    }
}
