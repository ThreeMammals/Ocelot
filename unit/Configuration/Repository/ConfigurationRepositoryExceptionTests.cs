using Ocelot.Configuration.Repository;
using Ocelot.Errors;

namespace Ocelot.UnitTests.Configuration.Repository;

public class ConfigurationRepositoryExceptionTests : UnitTest
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageCorrectly()
    {
        // Arrange
        const string expectedMessage = "Something went wrong with the configuration repository";

        // Act
        var exception = new ConfigurationRepositoryException(expectedMessage);

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Empty(exception.Data);
    }

    [Fact]
    public void Constructor_WithErrors_SetsMessageFromToErrorString_AndAddsErrorsToData()
    {
        // Arrange
        var errors = new List<Error>
        {
            new TestError("First error message", OcelotErrorCode.UnknownError, 500),
            new TestError("Second error message", OcelotErrorCode.UnableToFindDownstreamRouteError, 404)
        };

        // Act
        var exception = new ConfigurationRepositoryException(errors);

        // Assert
        Assert.Equal(errors.ToErrorString(), exception.Message);
        Assert.True(exception.Data.Contains(nameof(errors)));
        Assert.Same(errors, exception.Data[nameof(errors)]);
    }

    [Fact]
    public void Constructor_WithEmptyErrors_SetsEmptyErrorString()
    {
        // Arrange
        var errors = new List<Error>();

        // Act
        var exception = new ConfigurationRepositoryException(errors);

        // Assert
        Assert.Equal(errors.ToErrorString(), exception.Message);
        Assert.True(exception.Data.Contains(nameof(errors)));
    }

    // Concrete implementation for testing
    private class TestError : Error
    {
        public TestError(string message, OcelotErrorCode code, int statusCode)
            : base(message, code, statusCode)
        {
        }
    }
}
