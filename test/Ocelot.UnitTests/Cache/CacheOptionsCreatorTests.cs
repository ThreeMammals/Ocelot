using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Cache;

public class CacheOptionsCreatorTests : UnitTest
{
    private readonly CacheOptionsCreator _creator = new();

    [Fact]
    public void Should_create_region()
    {
        // Arrange
        var route = new FileRoute
        {
            UpstreamHttpMethod = new() { HttpMethods.Get },
            UpstreamPathTemplate = "/testdummy",
        };

        // Act
        var actual = _creator.Create(route.FileCacheOptions, new FileGlobalConfiguration(), route.UpstreamPathTemplate, route.UpstreamHttpMethod);

        // Assert
        actual.Region.ShouldBe($"{HttpMethods.Get}testdummy");
    }

    [Fact]
    public void Should_use_region()
    {
        // Arrange
        var route = new FileRoute
        {
            FileCacheOptions = new FileCacheOptions
            {
                Region = "region",
            },
        };

        // Act
        var actual = _creator.Create(route.FileCacheOptions, new FileGlobalConfiguration(), route.UpstreamPathTemplate, route.UpstreamHttpMethod);

        // Assert
        actual.Region.ShouldBe("region");
    }
}
