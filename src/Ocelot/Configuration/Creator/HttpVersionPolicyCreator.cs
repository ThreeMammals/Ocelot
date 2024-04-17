namespace Ocelot.Configuration.Creator;

/// <summary>
/// Default implementation of the <see cref="IVersionPolicyCreator"/> interface.
/// </summary>
public class HttpVersionPolicyCreator : IVersionPolicyCreator
{
    /// <summary>
    /// Creates a <see cref="HttpVersionPolicy"/> by a string.
    /// </summary>
    /// <param name="downstreamVersionPolicy">The string representation of the version policy.</param>
    /// <returns>An <see cref="HttpVersionPolicy"/> enumeration value.</returns>
    public HttpVersionPolicy Create(string downstreamVersionPolicy) => downstreamVersionPolicy switch
    {
        VersionPolicies.RequestVersionExact => HttpVersionPolicy.RequestVersionExact,
        VersionPolicies.RequestVersionOrHigher => HttpVersionPolicy.RequestVersionOrHigher,
        VersionPolicies.RequestVersionOrLower => HttpVersionPolicy.RequestVersionOrLower,
        _ => HttpVersionPolicy.RequestVersionOrLower,
    };
}
