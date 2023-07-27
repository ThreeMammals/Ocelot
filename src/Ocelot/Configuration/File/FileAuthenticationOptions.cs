namespace Ocelot.Configuration.File
{
    public class FileAuthenticationOptions
    {
        public FileAuthenticationOptions()
        {
            AllowedScopes = new List<string>();
        }

        public string AuthenticationProviderKey { get; set; }
        public List<string> AllowedScopes { get; set; }

        /// <summary>
        /// Allows anonymous authentication globally.
        /// <para>The property is significant only if the global AuthenticationOptions are used.</para>
        /// </summary>
        /// <value>
        /// <see langword="true"/> if it is allowed; otherwise, <see langword="false"/>.
        /// </value>
        public bool AllowAnonymousForGlobalAuthenticationOptions { get; set; }

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
