using Ocelot.Configuration.File;

namespace Ocelot.Configuration
{
    public sealed class AuthenticationOptions
    {
        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey)
        {
            AllowedScopes = allowedScopes;
            AuthenticationProviderKey = authenticationProviderKey;
            AuthenticationProviderKeys = [];
        }

        public AuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = from.AllowedScopes ?? [];
            AuthenticationProviderKey = from.AuthenticationProviderKey ?? string.Empty;
            AuthenticationProviderKeys = from.AuthenticationProviderKeys ?? [];
        }

        public AuthenticationOptions(List<string> allowedScopes, string authenticationProviderKey,
            string[] authenticationProviderKeys)
        {
            AllowedScopes = allowedScopes ?? [];
            AuthenticationProviderKey = authenticationProviderKey ?? string.Empty;
            AuthenticationProviderKeys = authenticationProviderKeys ?? [];
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
    }
}
