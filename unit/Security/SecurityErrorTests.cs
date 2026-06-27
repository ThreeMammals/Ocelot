using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Security;

namespace Ocelot.UnitTests.Security;

[Trait("Bug", "2403")] // https://github.com/ThreeMammals/Ocelot/issues/2403
[Trait("PR", "2406")] // https://github.com/ThreeMammals/Ocelot/pull/2406
public class SecurityErrorTests : UnitTest
{
    [Fact]
    public void Constructor_string()
    {
        // Arrange
        const string expectedMessage = "Access denied due to security policy violation";

        // Act
        var error = new SecurityError(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(OcelotErrorCode.SecurityError, error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, error.HttpStatusCode);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void Constructor_string_int ()
    {
        // Arrange
        const string expectedMessage = "Custom security error";
        const int customStatusCode = 401;

        // Act
        var error = new SecurityError(expectedMessage, customStatusCode);

        // Assert
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(OcelotErrorCode.SecurityError, error.Code);
        Assert.Equal(customStatusCode, error.HttpStatusCode);
        Assert.Null(error.Exception);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(429)]
    [InlineData(500)]
    public void Constructor_string_int_StatusCode(int statusCode)
    {
        // Arrange
        const string message = "Security violation";

        // Act
        var error = new SecurityError(message, statusCode);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.Equal(OcelotErrorCode.SecurityError, error.Code);
        Assert.Equal(statusCode, error.HttpStatusCode);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        const string message = "IP address blocked";
        var error = new SecurityError(message);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("SecurityError: IP address blocked", result);
    }

    [Fact]
    public void Inherits_From_Error_BaseClass()
    {
        // Act
        var error = new SecurityError("Test security error");

        // Assert
        Assert.IsAssignableFrom<Error>(error);
    }
}
