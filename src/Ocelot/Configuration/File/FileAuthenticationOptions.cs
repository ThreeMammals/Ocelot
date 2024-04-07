namespace Ocelot.Configuration.File
{
    public sealed class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new();
            AuthenticationProviderKeys = Array.Empty<string>();
            RequiredRole = new();
        }

        public FileAuthenticationOptions(FileAuthenticationOptions from)
        {
            AllowedScopes = new(from.AllowedScopes);
            AuthenticationProviderKey = from.AuthenticationProviderKey;
            AuthenticationProviderKeys = from.AuthenticationProviderKeys;
        }

        public List<string> AllowedScopes { get; set; }

        [Obsolete("Use the " + nameof(AuthenticationProviderKeys) + " property!")]
        public string AuthenticationProviderKey { get; set; }
        public string[] AuthenticationProviderKeys { get; set; }

        public List<string> RequiredRole { get; set; }
        public string ScopeKey { get; set; }
        public string RoleKey { get; set; }
        public string PolicyName { get; set; }

        public override string ToString() => new StringBuilder()
            .Append($"{nameof(AuthenticationProviderKey)}:'{AuthenticationProviderKey}',")
            .Append($"{nameof(AuthenticationProviderKeys)}:[{string.Join(',', AuthenticationProviderKeys.Select(x => $"'{x}'"))}],")
            .Append($"{nameof(AllowedScopes)}:[{string.Join(',', AllowedScopes.Select(x => $"'{x}'"))}]")
            .ToString();

        public string ToString2()
        {
            var sb = new StringBuilder();
            sb.Append($"{nameof(AuthenticationProviderKey)}:{AuthenticationProviderKey},{nameof(AllowedScopes)}:[");
            sb.AppendJoin(',', AllowedScopes);
            sb.Append("]");
            sb.Append($",{nameof(RequiredRole)}:[");
            sb.AppendJoin(',', RequiredRole);
            sb.Append("]");
            sb.Append($",{nameof(ScopeKey)}:[");
            sb.AppendJoin(',', ScopeKey);
            sb.Append("]");
            sb.Append($",{nameof(RoleKey)}:[");
            sb.AppendJoin(',', RoleKey);
            sb.Append("]");
            sb.Append($",{nameof(PolicyName)}:[");
            sb.AppendJoin(',', PolicyName);
            sb.Append("]");
            return sb.ToString();
        }
    }
}
