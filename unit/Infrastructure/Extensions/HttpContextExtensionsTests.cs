using Microsoft.AspNetCore.Http;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.UnitTests.Infrastructure.Extensions;

public class HttpContextExtensionsTests
{
    [Fact]
    public void IsOptionsMethod_ShouldReturnTrue_ForOptionsVerb()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethod.Options.Method; // "OPTIONS"

        // Act
        var result = context.IsOptionsMethod();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void IsOptionsMethod_ShouldReturnFalse_ForNonOptionsVerbs(string method)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = method;

        // Act
        var result = context.IsOptionsMethod();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOptionsMethod_ShouldBeCaseInsensitive()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = "options"; // lowercase

        // Act
        var result = context.IsOptionsMethod();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOptionsMethod_ShouldReturnFalse_WhenMethodIsEmpty()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Method = string.Empty;

        // Act
        var result = context.IsOptionsMethod();

        // Assert
        Assert.False(result);
    }
}
