using Newtonsoft.Json;
using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Configuration.FileModels;

public class FileAggregateRouteTests
{
    [Fact]
    [Trait("Feat", "79, 248")]
    public void Cstor_PropsAreInitialized()
    {
        // Arrange, Act
        var actual = new FileAggregateRoute();

        // Assert
        Assert.Equal(1, actual.Priority);
        Assert.Empty(actual.RouteKeys);
        Assert.Empty(actual.RouteKeysConfig);
        Assert.Empty(actual.UpstreamHeaderTemplates);
        Assert.Empty(actual.UpstreamHttpMethod);
    }

    [Fact]
    [Trait("Feat", "1389")]
    public void Cstor_UpstreamHttpMethod_ShouldBeEmpty()
    {
        // Arrange, Act
        var actual = new FileAggregateRoute();

        // Assert
        actual.UpstreamHttpMethod.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Theory]
    [Trait("Feat", "1389")]
    [InlineData(nameof(HttpMethod.Get))]
    [InlineData(nameof(HttpMethod.Post))]
    public void JsonDeserializationOfUpstreamHttpMethod_ShouldAllowHttpMethod(string verb)
    {
        // Arrange
        var json = $"{{\"{nameof(FileAggregateRoute.UpstreamHttpMethod)}\":[\"{verb}\"]}}";

        // Act
        FileAggregateRoute route = JsonConvert.DeserializeObject<FileAggregateRoute>(json);

        // Assert
        Assert.Contains(verb, route.UpstreamHttpMethod);
    }
}
