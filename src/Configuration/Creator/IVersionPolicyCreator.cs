namespace Ocelot.Configuration.Creator;

/// <summary>
/// Defines conversions from version policy strings to <see cref="HttpVersionPolicy"/> enumeration values.
/// </summary>
public interface IVersionPolicyCreator
{
    /// <summary>
    /// Creates a <see cref="HttpVersionPolicy"/> by a string.
    /// </summary>
    /// <param name="downstreamHttpVersionPolicy">The string representation of the version policy.</param>
    /// <returns>An <see cref="HttpVersionPolicy"/> enumeration value.</returns>
    HttpVersionPolicy Create(string downstreamHttpVersionPolicy);
}
