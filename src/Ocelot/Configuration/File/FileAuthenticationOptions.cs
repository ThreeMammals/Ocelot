namespace Ocelot.Configuration.File
{
    public class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new List<string>();
        }

        public FileAuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = new(from.AllowedScopes);
            AuthenticationProviderKey = from.AuthenticationProviderKey;
        }

        public string AuthenticationProviderKey { get; set; }
        public List<string> AllowedScopes { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{nameof(AuthenticationProviderKey)}:{AuthenticationProviderKey},{nameof(AllowedScopes)}:[");
            sb.AppendJoin(',', AllowedScopes);
            sb.Append(']');
            return sb.ToString();
        }
    }
}
