using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public sealed class SecurityOptionsCreatorTests : UnitTest
{
    private readonly SecurityOptionsCreator _creator = new();

    [Fact]
    public void Should_create_route_security_config()
    {
        // Arrange
        var ipAllowedList = new List<string> { "127.0.0.1", "192.168.1.1" };
        var ipBlockedList = new List<string> { "127.0.0.1", "192.168.1.1" };
        var securityOptions = new FileSecurityOptions
        {
            IPAllowedList = ipAllowedList,
            IPBlockedList = ipBlockedList,
        };
        var expected = new SecurityOptions(ipAllowedList, ipBlockedList);
        var globalConfig = new FileGlobalConfiguration();

        // Act
        var actual = _creator.Create(securityOptions, globalConfig);

        // Assert
        actual.ThenTheResultIs(expected);
    }

    [Fact]
    [Trait("Feat", "2170")]
    public void Should_create_global_security_config()
    {
        // Arrange
        var ipAllowedList = new List<string> { "127.0.0.1", "192.168.1.1" };
        var ipBlockedList = new List<string> { "127.0.0.1", "192.168.1.1" };
        var globalConfig = new FileGlobalConfiguration
        {
            SecurityOptions = new()
            {
                IPAllowedList = ipAllowedList,
                IPBlockedList = ipBlockedList,
            },
        };
        var expected = new SecurityOptions(ipAllowedList, ipBlockedList);

        // Act
        var actual = _creator.Create(new(), globalConfig);

        // Assert
        actual.ThenTheResultIs(expected);
    }

    [Fact]
    [Trait("Feat", "2170")]
    public void Should_create_global_route_security_config()
    {
        // Arrange
        var routeIpAllowedList = new List<string> { "127.0.0.1", "192.168.1.1" };
        var routeIpBlockedList = new List<string> { "127.0.0.1", "192.168.1.1" };
        var securityOptions = new FileSecurityOptions
        {
            IPAllowedList = routeIpAllowedList,
            IPBlockedList = routeIpBlockedList,
        };
        var globalIpAllowedList = new List<string> { "127.0.0.2", "192.168.1.2" };
        var globalIpBlockedList = new List<string> { "127.0.0.2", "192.168.1.2" };
        var globalConfig = new FileGlobalConfiguration
        {
            SecurityOptions = new FileSecurityOptions
            {
                IPAllowedList = globalIpAllowedList,
                IPBlockedList = globalIpBlockedList,
            },
        };
        var expected = new SecurityOptions(routeIpAllowedList, routeIpBlockedList);

        // Act
        var actual = _creator.Create(securityOptions, globalConfig);

        // Assert
        actual.ThenTheResultIs(expected);
    }
}

internal static class SecurityOptionsExtensions
{
    public static void ThenTheResultIs(this SecurityOptions actual, SecurityOptions expected)
    {
        for (var i = 0; i < expected.IPAllowedList.Count; i++)
        {
            actual.IPAllowedList[i].ShouldBe(expected.IPAllowedList[i]);
        }

        for (var i = 0; i < expected.IPBlockedList.Count; i++)
        {
            actual.IPBlockedList[i].ShouldBe(expected.IPBlockedList[i]);
        }
    }
}
