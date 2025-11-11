namespace Ocelot.Configuration.File;

public class FileAuthenticationOptions
{
    public FileAuthenticationOptions()
    {
        AllowedScopes = new();
    }

    public FileAuthenticationOptions(string authScheme) : this()
        => AuthenticationProviderKeys = [authScheme];

    public FileAuthenticationOptions(FileAuthenticationOptions from)
    {
        ArgumentNullException.ThrowIfNull(from, nameof(from));
        AllowAnonymous = from.AllowAnonymous;
        AllowedScopes = new(from.AllowedScopes);
        AuthenticationProviderKey = from.AuthenticationProviderKey;
        AuthenticationProviderKeys = new string[from.AuthenticationProviderKeys.Length];
        Array.Copy(from.AuthenticationProviderKeys, AuthenticationProviderKeys, from.AuthenticationProviderKeys.Length);
    }

    public List<string> AllowedScopes { get; set; }

    /// <summary>Allows anonymous authentication for route when global authentication options are used.</summary>
    /// <value><see langword="true"/> if it is allowed; otherwise, <see langword="false"/>.</value>
    public bool? AllowAnonymous { get; set; }

    [Obsolete("Use AuthenticationProviderKeys instead of AuthenticationProviderKey! Note that AuthenticationProviderKey will be removed in version 25.0!")]
    public string AuthenticationProviderKey { get; set; }

    public string[] AuthenticationProviderKeys { get; set; }

    /// <summary>Checks whether authentication schemes are specified (not empty, exist).</summary>
    /// <value><see langword="true"/> if an authentication scheme is defined; otherwise, <see langword="false"/>.</value>
    public bool HasScheme => !string.IsNullOrWhiteSpace(AuthenticationProviderKey)
            || AuthenticationProviderKeys?.Any(k => !string.IsNullOrWhiteSpace(k)) == true;
    public bool HasScope => AllowedScopes.Exists(s => !string.IsNullOrWhiteSpace(s));

    public override string ToString() => new StringBuilder()
        .Append($"{nameof(AllowAnonymous)}:{AllowAnonymous ?? false},")
        .Append($"{nameof(AllowedScopes)}:[{string.Join(',', AllowedScopes?.Select(x => $"'{x}'") ?? Enumerable.Empty<string>())}],")
        .Append($"{nameof(AuthenticationProviderKey)}:'{AuthenticationProviderKey}',")
        .Append($"{nameof(AuthenticationProviderKeys)}:[{string.Join(',', AuthenticationProviderKeys?.Select(x => $"'{x}'") ?? Enumerable.Empty<string>())}]")
        .ToString();
}
