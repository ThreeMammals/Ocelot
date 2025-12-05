using Microsoft.Extensions.Primitives;
using Ocelot.Tracing.Butterfly;

namespace Ocelot.UnitTests.Tracing;

public class ButterflyStringValuesExtensionsTests
{
    [Fact]
    public void GetValue_ShouldReturnSingleValue_WhenCountIsOne()
    {
        // Arrange
        var values = new StringValues("only");

        // Act
        var result = values.GetValue();

        // Assert
        Assert.Equal("only", result);
    }

    [Fact]
    public void GetValue_ShouldReturnLastValue_WhenMultipleValues()
    {
        // Arrange
        var values = new StringValues(["first", "second", "third"]);

        // Act
        var result = values.GetValue();

        // Assert
        Assert.Equal("third", result);
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenEmpty()
    {
        // Arrange
        var values = StringValues.Empty;

        // Act
        var result = values.GetValue();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_ShouldHandleTwoValues()
    {
        // Arrange
        var values = new StringValues(["alpha", "beta"]);

        // Act
        var result = values.GetValue();

        // Assert
        Assert.Equal("beta", result);
    }

    [Fact]
    public void GetValue_ShouldReturnNull_WhenCreatedWithNull()
    {
        // Arrange
        var values = new StringValues((string)null);

        // Act
        var result = values.GetValue();

        // Assert
        Assert.Null(result);
    }
}
