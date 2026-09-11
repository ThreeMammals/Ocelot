using Microsoft.AspNetCore.Http;
using Ocelot.Errors;

namespace Ocelot.UnitTests.Errors;

public class UnknownErrorTests
{
    [Fact]
    public void DefaultConstructor_SetsCorrectProperties()
    {
        // Act
        var error = new UnknownError();

        // Assert
        Assert.Equal(string.Empty, error.Message);
        Assert.Equal(OcelotErrorCode.UnknownError, error.Code);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.HttpStatusCode);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void MessageConstructor_SetsCorrectProperties()
    {
        // Arrange
        const string expectedMessage = "An unknown error occurred during configuration loading";

        // Act
        var error = new UnknownError(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(OcelotErrorCode.UnknownError, error.Code);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.HttpStatusCode);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void ExceptionConstructor_SetsCorrectProperties()
    {
        // Arrange
        var innerException = new InvalidOperationException("Test inner exception");

        // Act
        var error = new UnknownError(innerException);

        // Assert
        Assert.Equal("Test inner exception", error.Message); // Assuming base uses exception.Message
        Assert.Equal(OcelotErrorCode.UnknownError, error.Code);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.HttpStatusCode);
        Assert.Same(innerException, error.Exception);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat_ForMessageConstructor()
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
    public void ToString_ReturnsCorrectFormat_ForDefaultConstructor()
    {
        // Arrange
        var error = new UnknownError();

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("UnknownError: ", result);
    }

    [Fact]
    public void Inherits_From_Error_BaseClass()
    {
        // Act
        var error = new UnknownError("Test message");

        // Assert
        Assert.IsAssignableFrom<Error>(error);
    }

    [Fact]
    public void ExceptionConstructor_NullArgument_ThrownArgumentNullException()
    {
        // Arrange
        Exception exception = null;

        // Act
        var actual = Assert.Throws<ArgumentNullException>(
            () => new UnknownError(exception));

        // Assert
        Assert.Equal(nameof(exception), actual.ParamName);
    }
}
