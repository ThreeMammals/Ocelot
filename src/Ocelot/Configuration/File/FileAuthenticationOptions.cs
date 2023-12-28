namespace Ocelot.Configuration.File
{
    public sealed class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new List<string>();
            AuthenticationProviderKeys = Array.Empty<string>();
        }

        public FileAuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = new(from.AllowedScopes);
            AuthenticationProviderKey = from.AuthenticationProviderKey;
            AuthenticationProviderKeys = from.AuthenticationProviderKeys;
        }

        public List<string> AllowedScopes { get; set; }
        public string AuthenticationProviderKey { get; set; }
        public string[] AuthenticationProviderKeys { get; set; }

        public override string ToString() => new StringBuilder()
             .Append($"{nameof(AuthenticationProviderKey)}:{AuthenticationProviderKey},")
             .Append($"{nameof(AuthenticationProviderKeys)}:[{string.Join(',', AuthenticationProviderKeys)}],")
             .Append($"{nameof(AllowedScopes)}:[{string.Join(',', AllowedScopes)}]")
             .ToString();
    }
}
