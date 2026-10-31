using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.DownstreamUrlCreator;
using Ocelot.Request.Middleware;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamUrlCreator;

public class DefaultDownstreamPathFormatterTests : UnitTest
{
    [Fact]
    public void TryFormat_should_return_false_without_modifying_request()
    {
        // Arrange
        var formatter = new DefaultDownstreamPathFormatter();
        var downstreamRequest = new DownstreamRequest
        {
            AbsolutePath = "/original/path",
        };
        var downstreamRoute = new DownstreamRouteBuilder().Build();
        var configuration = new InternalConfiguration();
        var placeholders = Array.Empty<PlaceholderNameAndValue>();
        var downstreamPath = new DownstreamPath("/downstream/path");

        // Act
        var result = formatter.TryFormat(
            downstreamRequest,
            downstreamRoute,
            configuration,
            placeholders,
            downstreamPath);

        // Assert
        result.ShouldBeFalse();
        downstreamRequest.AbsolutePath.ShouldBe("/original/path");
    }
}
