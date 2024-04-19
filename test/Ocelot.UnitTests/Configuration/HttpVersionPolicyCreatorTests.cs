using Ocelot.Configuration.Creator;

namespace Ocelot.UnitTests.Configuration;

[Trait("Feat", "1672")]
public sealed class HttpVersionPolicyCreatorTests : UnitTest
{
    private readonly HttpVersionPolicyCreator _creator;

    public HttpVersionPolicyCreatorTests()
    {
        _creator = new HttpVersionPolicyCreator();
    }

    [Theory]
    [InlineData(VersionPolicies.RequestVersionOrLower, HttpVersionPolicy.RequestVersionOrLower)]
    [InlineData(VersionPolicies.RequestVersionExact, HttpVersionPolicy.RequestVersionExact)]
    [InlineData(VersionPolicies.RequestVersionOrHigher, HttpVersionPolicy.RequestVersionOrHigher)]
    public void Should_create_version_policy_based_on_input(string versionPolicy, HttpVersionPolicy expected)
    {
        // Arrange, Act
        var actual = _creator.Create(versionPolicy);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid version")]
    public void Should_default_to_request_version_or_lower(string versionPolicy)
    {
        // Arrange, Act
        var actual = _creator.Create(versionPolicy);

        // Assert
        Assert.Equal(HttpVersionPolicy.RequestVersionOrLower, actual);
    }

    [Fact]
    public void Should_default_to_request_version_or_lower_when_setting_gibberish()
    {
        // Arrange, Act
        var actual = _creator.Create("string is gibberish");

        // Assert
        Assert.Equal(HttpVersionPolicy.RequestVersionOrLower, actual);
    }
}
