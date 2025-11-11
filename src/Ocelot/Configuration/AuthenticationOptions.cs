using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration;

public sealed class AuthenticationOptions
{
    public AuthenticationOptions()
    {
        AllowedScopes = new();
        AuthenticationProviderKeys = Array.Empty<string>();
    }

    public AuthenticationOptions(FileAuthenticationOptions from)
    {
        AllowedScopes = from.AllowedScopes ?? new();
        AuthenticationProviderKeys = Merge(from.AuthenticationProviderKey, from.AuthenticationProviderKeys ?? Array.Empty<string>());
    }

    public AuthenticationOptions(List<string> allowedScopes, string[] authenticationProviderKeys)
    {
        AllowedScopes = allowedScopes ?? new();
        AuthenticationProviderKeys = authenticationProviderKeys ?? Array.Empty<string>();
    }

    private static string[] Merge(string primaryKey, string[] keys)
    {
        if (primaryKey.IsEmpty())
        {
            return keys;
        }

        List<string> merged = new(1 + keys.Length) { primaryKey };
        merged.AddRange(keys);
        return merged.ToArray();
    }

    public List<string> AllowedScopes { get; init; }

    /// <summary>
    /// Multiple authentication schemes registered in DI services with appropriate authentication providers.
    /// </summary>
    /// <remarks>
    /// The order in the collection matters: first successful authentication result wins.
    /// </remarks>
    /// <value>
    /// An array of <see langword="string"/> values of the scheme names.
    /// </value>
    public string[] AuthenticationProviderKeys { get; init; }
}
