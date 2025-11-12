using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration;

public sealed class AuthenticationOptions
{
    public AuthenticationOptions()
    {
        AllowAnonymous = false;
        AllowedScopes = new();
        AuthenticationProviderKeys = Array.Empty<string>();
    }

    public AuthenticationOptions(FileAuthenticationOptions from)
    {
        AllowAnonymous = from.AllowAnonymous ?? false;
        AllowedScopes = from.AllowedScopes ?? new();
        AuthenticationProviderKeys = Merge(from.AuthenticationProviderKey, from.AuthenticationProviderKeys ?? Array.Empty<string>());
    }

    public AuthenticationOptions(List<string> allowedScopes, string[] authenticationProviderKeys)
    {
        AllowAnonymous = false;
        AllowedScopes = allowedScopes ?? new();
        AuthenticationProviderKeys = authenticationProviderKeys ?? Array.Empty<string>();
    }

    public static string[] Merge(string primaryKey, string[] keys)
    {
        if (primaryKey.IsEmpty())
        {
            return keys;
        }

        List<string> merged = new(1 + keys.Length) { primaryKey };
        merged.AddRange(keys);
        return merged.ToArray();
    }

    /// <summary>Allows anonymous authentication for route when global authentication options are used.</summary>
    /// <value><see langword="true"/> if it is allowed; otherwise, <see langword="false"/>.</value>
    public bool AllowAnonymous { get; init; }
    public List<string> AllowedScopes { get; init; }

    /// <summary>Multiple authentication schemes registered in DI services with appropriate authentication providers.</summary>
    /// <remarks>The order in the collection matters: first successful authentication result wins.</remarks>
    /// <value>An array of <see langword="string"/> values of the scheme names.</value>
    public string[] AuthenticationProviderKeys { get; init; }

    public bool HasScheme => AuthenticationProviderKeys.Any(k => !k.IsEmpty());
}
