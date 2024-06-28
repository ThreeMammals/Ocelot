namespace Ocelot.Configuration.File
{
    public sealed class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new();
            AuthenticationProviderKeys = Array.Empty<string>();
        }

        public FileAuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = new(from.AllowedScopes);
            AuthenticationProviderKey = from.AuthenticationProviderKey;
            AuthenticationProviderKeys = from.AuthenticationProviderKeys;
        }

        public List<string> AllowedScopes { get; set; }

        /// <summary>
        /// Allows anonymous authentication for route when global AuthenticationOptions are used.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if it is allowed; otherwise, <see langword="false"/>.
        /// </value>
        public bool AllowAnonymous { get; set; }

        [Obsolete("Use the " + nameof(AuthenticationProviderKeys) + " property!")]
        public string AuthenticationProviderKey { get; set; }

        public string[] AuthenticationProviderKeys { get; set; }

        public bool HasProviderKey() => !string.IsNullOrEmpty(AuthenticationProviderKey)
                || AuthenticationProviderKeys?.Any(k => !string.IsNullOrWhiteSpace(k)) == true;

        public override string ToString() => new StringBuilder()
            .Append($"{nameof(AuthenticationProviderKey)}:'{AuthenticationProviderKey}',")
            .Append($"{nameof(AuthenticationProviderKeys)}:[{string.Join(',', AuthenticationProviderKeys.Select(x => $"'{x}'"))}],")
            .Append($"{nameof(AllowedScopes)}:[{string.Join(',', AllowedScopes.Select(x => $"'{x}'"))}]")
            .ToString();
    }
}
