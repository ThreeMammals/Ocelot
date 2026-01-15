using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration;

public class RequestIdKeyCreatorTests : UnitTest
{
    private readonly RequestIdKeyCreator _creator = new();

    [Fact]
    public void Should_use_global_configuration()
    {
        // Arrange
        var route = new FileRoute();
        var globalConfig = new FileGlobalConfiguration
        {
            RequestIdKey = "cheese",
        };

        // Act
        var result = _creator.Create(route, globalConfig);

        // Assert
        result.ShouldBe("cheese");
    }

    [Fact]
    public void Should_use_re_route_specific()
    {
        // Arrange
        var route = new FileRoute
        {
            RequestIdKey = "cheese",
        };
        var globalConfig = new FileGlobalConfiguration();

        // Act
        var result = _creator.Create(route, globalConfig);

        // Assert
        result.ShouldBe("cheese");
    }

    [Fact]
    public void Should_use_re_route_over_global_specific()
    {
        // Arrange
        var route = new FileRoute
        {
            RequestIdKey = "cheese",
        };
        var globalConfig = new FileGlobalConfiguration
        {
            RequestIdKey = "test",
        };

        // Act
        var result = _creator.Create(route, globalConfig);

        // Assert
        result.ShouldBe("cheese");
    }
}
