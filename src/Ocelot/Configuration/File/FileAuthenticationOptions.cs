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
        }

        public List<string> AllowedScopes { get; set; }

        public string AuthenticationProviderKey { get; set; }

        public string[] AuthenticationProviderKeys { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"{nameof(AuthenticationProviderKey)}:{AuthenticationProviderKey},{nameof(AuthenticationProviderKeys)}:[");
            sb.AppendJoin(',', AuthenticationProviderKeys);
            sb.Append("],");
            sb.Append($"{nameof(AllowedScopes)}:[");
            sb.AppendJoin(',', AllowedScopes);
            sb.Append(']');
            return sb.ToString();
        }
    }
}
