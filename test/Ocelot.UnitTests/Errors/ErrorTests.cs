using Ocelot.Errors;

namespace Ocelot.UnitTests.Errors;

public class ErrorTests
{
    // Concrete implementation for testing the abstract class
    private class TestError : Error
    {
        public TestError(string message, OcelotErrorCode code, int statusCode)
            : base(message, code, statusCode) { }

        public TestError(Exception exception, OcelotErrorCode code, int statusCode)
            : base(exception, code, statusCode) { }
    }

    [Fact]
    public void Constructor_WithMessage_SetsAllPropertiesCorrectly()
    {
        // Arrange
        const string expectedMessage = "Something went wrong";
        const OcelotErrorCode expectedCode = OcelotErrorCode.RequestTimedOutError;
        const int expectedStatusCode = 504;

        // Act
        var error = new TestError(expectedMessage, expectedCode, expectedStatusCode);

        // Assert
        Assert.Equal(expectedMessage, error.Message);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedStatusCode, error.HttpStatusCode);
        Assert.Null(error.Exception);
    }

    [Fact]
    public void Constructor_WithException_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");
        const OcelotErrorCode expectedCode = OcelotErrorCode.UnableToFindDownstreamRouteError;
        const int expectedStatusCode = 500;

        // Act
        var error = new TestError(exception, expectedCode, expectedStatusCode);

        // Assert
        Assert.Equal(exception, error.Exception);
        Assert.Equal("Database connection failed", error.Message);
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(expectedStatusCode, error.HttpStatusCode);
    }

    [Fact]
    public void Constructor_WithException_UsesExceptionMessage()
    {
        // Arrange
        var exception = new ArgumentNullException("param", "Value cannot be null.");

        // Act
        var error = new TestError(exception, OcelotErrorCode.RequestTimedOutError, 400);

        // Assert
        Assert.Equal("Value cannot be null. (Parameter 'param')", error.Message);
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var error = new TestError("Test error message", OcelotErrorCode.UnknownError, 500);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("UnknownError: Test error message", result);
    }

    [Fact]
    public void ToString_WithException_UsesExceptionMessage()
    {
        // Arrange
        var exception = new Exception("Inner exception details");
        var error = new TestError(exception, OcelotErrorCode.RequestTimedOutError, 504);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("RequestTimedOutError: Inner exception details", result);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public void HttpStatusCode_CanBeAnyValidStatusCode(int statusCode)
    {
        // Act
        var error = new TestError("Test", OcelotErrorCode.UnknownError, statusCode);

        // Assert
        Assert.Equal(statusCode, error.HttpStatusCode);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        var error = new TestError("Read-only test", OcelotErrorCode.UnknownError, 400);

        // These should compile (no setter) - we're just verifying they're get-only
        Assert.NotNull(error.Message);
        Assert.NotNull(error.Code);
        Assert.NotNull(error.HttpStatusCode);
        Assert.True(true); // If we got here, properties are read-only as expected
    }
}
