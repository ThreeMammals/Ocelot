using Ocelot.Configuration.File;
using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public sealed class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, List<string> requiredRole, string authenticationProviderKey, string scopeKey, string roleKey, string policyName)
        {
            PolicyName = policyName;
            AllowedScopes = allowedScopes;
            RequiredRole = requiredRole;
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

        /// <summary>
        /// Authentication scheme registered in DI services with appropriate authentication provider.
        /// </summary>
        /// <value>
        /// A <see langword="string"/> value of the scheme name.
        /// </value>
        [Obsolete("Use the " + nameof(AuthenticationProviderKeys) + " property!")]
        public string AuthenticationProviderKey { get; }

        /// <summary>
        /// Multiple authentication schemes registered in DI services with appropriate authentication providers.
        /// </summary>
        /// <remarks>
        /// The order in the collection matters: first successful authentication result wins.
        /// </remarks>
        /// <value>
        /// An array of <see langword="string"/> values of the scheme names.
        /// </value>
        public string[] AuthenticationProviderKeys { get; }

        public List<string> RequiredRole { get; }
        public string ScopeKey { get; }
        public string RoleKey { get; }
        public string PolicyName { get; }
    }
}
