namespace Ocelot.Configuration.Creator;

/// <summary>
/// Constants for conversions in concrete classes for the <see cref="IVersionPolicyCreator"/> interface.
/// </summary>
public class VersionPolicies
{
    public const string RequestVersionExact = nameof(RequestVersionExact);
    public const string RequestVersionOrLower = nameof(RequestVersionOrLower);
    public const string RequestVersionOrHigher = nameof(RequestVersionOrHigher);
}
