using Ocelot.Errors;
using System.Text;

namespace Ocelot.UnitTests.Errors;

public class ExceptionExtensionsTests : UnitTest
{
    [Fact]
    public void AllMessages_WithNullException_ReturnsEmptyBuilder()
    {
        // Arrange
        Exception ex = null;

        // Act
        var actual = ex.AllMessages();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(string.Empty, actual.ToString());
    }

    [Fact]
    public void AllMessages_WithNoInnerException_AppendsMessage()
    {
        // Arrange
        var ex = new InvalidOperationException("Test message");

        // Act
        var actual = ex.AllMessages();

        // Assert
        Assert.Equal("Test message" + NL, actual.ToString());
    }

    [Fact]
    public void AllMessages_WithInnerException_AppendsAllMessagesRecursively()
    {
        // Arrange
        var innerMost = new ArgumentException("Innermost message");
        var middle = new InvalidOperationException("Middle message", innerMost);
        var outer = new Exception("Outer message", middle);

        // Act
        var actual = outer.AllMessages();

        // Assert
        var expected = $"Outer message{NL}Middle message{NL}Innermost message{NL}";
        Assert.Equal(expected, actual.ToString());
    }

    [Fact]
    public void AllMessages_ReusesProvidedBuilder()
    {
        // Arrange
        var ex = new Exception("Test message");
        var builder = new StringBuilder("Pre-existing: ");

        // Act
        var actual = ex.AllMessages(builder);

        // Assert
        Assert.Same(builder, actual);
        Assert.Equal("Pre-existing: Test message" + NL, actual.ToString());
    }

    [Fact]
    public void GetMessages_ReturnsAllMessagesAsString()
    {
        // Arrange
        var inner = new Exception("Inner");
        var outer = new Exception("Outer", inner);

        // Act
        var actual = outer.GetMessages();

        // Assert
        var expected = $"Outer{NL}Inner{NL}";
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetMessages_WithNullException_ReturnsEmptyString()
    {
        // Act
        var actual = ((Exception)null).GetMessages();

        // Assert
        Assert.Equal(string.Empty, actual);
    }

    [Fact]
    public void ToShortString_ReturnsFormattedString_WithTypeAndMessages()
    {
        // Arrange
        var inner = new ArgumentNullException("param");
        var outer = new InvalidOperationException("Operation failed", inner);

        // Act
        var actual = outer.ToShortString();

        // Assert
        var expectedMessages = $"Operation failed{NL}Value cannot be null. (Parameter 'param'){NL}";
        var expected = $"InvalidOperationException: {expectedMessages}";
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToShortString_WithNullException_ReturnsEmpty()
    {
        // Act
        var actual = ((Exception)null).ToShortString();

        // Assert
        Assert.Equal(string.Empty, actual); // Note: calling on null will throw, but extension handles via GetMessages
    }

    [Fact]
    public void ToShortString_WithSimpleException()
    {
        // Arrange
        var ex = new UnauthorizedAccessException("No access!");

        // Act
        var actual = ex.ToShortString();

        // Assert
        Assert.Equal($"UnauthorizedAccessException: No access!{NL}", actual);
    }
}
