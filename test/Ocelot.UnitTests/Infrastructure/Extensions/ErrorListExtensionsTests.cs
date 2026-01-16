using Ocelot.Errors;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.UnitTests.Infrastructure.Extensions;

public class ErrorListExtensionsTests
{
    private static readonly string NL = Environment.NewLine;

    [Fact]
    public void ToErrorString_ShouldReturnEmptyString_WhenListIsEmpty()
    {
        // Arrange
        var errors = new List<Error>();

        // Act
        var result = errors.ToErrorString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToErrorString_ShouldReturnSingleError_WhenListHasOneItem()
    {
        // Arrange
        var errors = new List<Error> { new CannotAddPlaceholderError("First error") };

        // Act
        var result = errors.ToErrorString();

        // Assert
        Assert.Equal("CannotAddPlaceholderError: First error", result);
    }

    [Fact]
    public void ToErrorString_ShouldJoinMultipleErrors_WithNewLine()
    {
        // Arrange
        var errors = new List<Error>
            {
                new CannotAddPlaceholderError("First"),
                new CannotAddPlaceholderError("Second"),
                new CannotAddPlaceholderError("Third"),
            };

        // Act
        var result = errors.ToErrorString();

        // Assert
        var expected = $"CannotAddPlaceholderError: First{NL}CannotAddPlaceholderError: Second{NL}CannotAddPlaceholderError: Third";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToErrorString_ShouldInsertNewLineBefore_WhenBeforeIsTrue()
    {
        // Arrange
        var errors = new List<Error> { new CannotAddPlaceholderError("First") };

        // Act
        var result = errors.ToErrorString(before: true);

        // Assert
        var expected = $"{NL}CannotAddPlaceholderError: First";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToErrorString_ShouldInsertNewLineAfter_WhenAfterIsTrue()
    {
        // Arrange
        var errors = new List<Error> { new CannotAddPlaceholderError("First") };

        // Act
        var result = errors.ToErrorString(after: true);

        // Assert
        var expected = $"CannotAddPlaceholderError: First{NL}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToErrorString_ShouldInsertNewLineBeforeAndAfter_WhenBothFlagsTrue()
    {
        // Arrange
        var errors = new List<Error> { new CannotAddPlaceholderError("First") };

        // Act
        var result = errors.ToErrorString(before: true, after: true);

        // Assert
        var expected = $"{NL}CannotAddPlaceholderError: First{NL}";
        Assert.Equal(expected, result);
    }
}
