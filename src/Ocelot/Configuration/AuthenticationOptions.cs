using Ocelot.Configuration.File;
using System;

namespace Ocelot.Configuration
{
    public sealed class AuthenticationOptions
    {
        public AuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = from.AllowedScopes ?? new();
            BuildAuthenticationProviderKeys(from.AuthenticationProviderKey, from.AuthenticationProviderKeys);
            PolicyName = from.PolicyName;
            RequiredRole = from.RequiredRole;
            ScopeKey = from.ScopeKey;
            RoleKey = from.RoleKey;
        }

        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey, string[] authenticationProviderKeys)
        {
            AllowedScopes = allowedScopes ?? new();
            BuildAuthenticationProviderKeys(authenticationProviderKey, authenticationProviderKeys);
        }

        public AuthenticationOptions(List<string> allowedScopes, string[] authenticationProviderKeys, List<string> requiredRole, string scopeKey, string roleKey, string policyName)
        {
            AllowedScopes = allowedScopes;
            AuthenticationProviderKey = string.Empty;
            AuthenticationProviderKeys = authenticationProviderKeys ?? Array.Empty<string>();
            PolicyName = policyName;
            RequiredRole = requiredRole;
            ScopeKey = scopeKey;
            RoleKey = roleKey;
        }

        /// <summary>
        /// Builds auth keys migrating legacy key to new ones.
        /// </summary>
        /// <param name="legacyKey">The legacy <see cref="AuthenticationProviderKey"/>.</param>
        /// <param name="keys">New <see cref="AuthenticationProviderKeys"/> to build.</param>
        private void BuildAuthenticationProviderKeys(string legacyKey, string[] keys)
        {
            keys ??= new string[];
            if (string.IsNullOrEmpty(legacyKey))
            {
                return;
            }

            // Add legacy Key to new Keys array as the first element
            var arr = new string[keys.Length + 1];
            arr[0] = legacyKey;
            Array.Copy(keys, 0, arr, 1, keys.Length);

            // Update the object
            AuthenticationProviderKeys = arr;
            AuthenticationProviderKey = string.Empty;
        }

        public List<string> AllowedScopes { get; }

        /// <summary>
        /// Authentication scheme registered in DI services with appropriate authentication provider.
        /// </summary>
        /// <value>
        /// A <see langword="string"/> value of the scheme name.
        /// </value>
        [Obsolete("Use the " + nameof(AuthenticationProviderKeys) + " property!")]
        public string AuthenticationProviderKey { get; private set; }

        /// <summary>
        /// Multiple authentication schemes registered in DI services with appropriate authentication providers.
        /// </summary>
        /// <remarks>
        /// The order in the collection matters: first successful authentication result wins.
        /// </remarks>
        /// <value>
        /// An array of <see langword="string"/> values of the scheme names.
        /// </value>
        public string[] AuthenticationProviderKeys { get; private set; }

        public List<string> RequiredRole { get; }
        public string ScopeKey { get; }
        public string RoleKey { get; }
        public string PolicyName { get; }
    }
}
