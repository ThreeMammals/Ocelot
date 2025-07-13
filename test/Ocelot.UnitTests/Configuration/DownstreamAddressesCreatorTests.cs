using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class DownstreamAddressesCreatorTests : UnitTest
{
    public readonly DownstreamAddressesCreator _creator;

    public DownstreamAddressesCreatorTests()
    {
        _creator = new DownstreamAddressesCreator();
    }

    [Fact]
    public void Should_do_nothing()
    {
        // Arrange
        var route = new FileRoute();
        var expected = new List<DownstreamHostAndPort>();

        // Act
        var result = _creator.Create(route);

        // Assert
        result.TheThenFollowingIsReturned(expected);
    }

    [Fact]
    public void Should_create_downstream_addresses_from_old_downstream_path_and_port()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHostAndPorts = new List<FileHostAndPort>
            {
                new("test", 80),
            },
        };
        var expected = new List<DownstreamHostAndPort>
        {
            new("test", 80),
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.TheThenFollowingIsReturned(expected);
    }

    [Fact]
    public void Should_create_downstream_addresses_from_downstream_host_and_ports()
    {
        // Arrange
        var route = new FileRoute
        {
            DownstreamHostAndPorts = new List<FileHostAndPort>
            {
                new("test", 80),
                new("west", 443),
            },
        };
        var expected = new List<DownstreamHostAndPort>
        {
            new("test", 80),
            new("west", 443),
        };

        // Act
        var result = _creator.Create(route);

        // Assert
        result.TheThenFollowingIsReturned(expected);
    }
}

internal static class ListOfDownstreamHostAndPortExtensions
{
    public static void TheThenFollowingIsReturned(this List<DownstreamHostAndPort> actual, List<DownstreamHostAndPort> expecteds)
    {
        actual.Count.ShouldBe(expecteds.Count);

        for (var i = 0; i < actual.Count; i++)
        {
            var result = actual[i];
            var expected = expecteds[i];

            result.Host.ShouldBe(expected.Host);
            result.Port.ShouldBe(expected.Port);
        }
    }
}
