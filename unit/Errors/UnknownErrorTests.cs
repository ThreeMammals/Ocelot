using Ocelot.Errors;

namespace Ocelot.UnitTests.Errors;

public class UnknownErrorTests
{
    [Fact]
    public void Constructor_SetsCorrectErrorCode_AndStatusCode()
    {
        // Arrange
        const string expectedMessage = "An unknown error occurred during configuration loading";

        // Act
        var error = new UnknownError(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(OcelotErrorCode.UnknownError, error.Code);
        Assert.Equal(0, error.HttpStatusCode);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        const string message = "Test unknown error";
        var error = new UnknownError(message);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("UnknownError: Test unknown error", result);
    }

    [Fact]
    public void Inherits_From_Error_BaseClass()
    {
        // Act
        var error = new UnknownError("Test message");

        // Assert
        Assert.IsAssignableFrom<Error>(error);
    }
}
