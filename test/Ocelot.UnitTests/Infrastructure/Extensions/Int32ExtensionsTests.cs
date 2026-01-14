using Ocelot.Infrastructure.Extensions;

namespace Ocelot.UnitTests.Infrastructure.Extensions;

public class Int32ExtensionsTests
{
    // ---------------------------
    // Tests for Ensure
    // ---------------------------
    [Theory]
    [InlineData(-5, 0, 0)]// below default low
    [InlineData(5, 0, 5)] // above default low
    [InlineData(3, 2, 3)] // above custom low
    [InlineData(1, 5, 5)] // below custom low
    [InlineData(5, 5, 5)] // equal to low
    public void Ensure_ShouldReturnExpectedValue(int value, int low, int expected)
    {
        // Act
        var result = value.Ensure(low);

        // Assert
        Assert.True(result >= low);
        Assert.Equal(expected, result);
    }

    // ---------------------------
    // Tests for Positive (int)
    // ---------------------------
    [Theory]
    [InlineData(-10, 1)]// negative becomes 1
    [InlineData(0, 1)] // zero becomes 1
    [InlineData(5, 5)] // positive stays same
    public void Positive_Int_ShouldReturnPositiveValue(int value, int expected)
    {
        // Act
        var result = value.Positive();

        // Assert
        Assert.True(result > 0);
        Assert.Equal(expected, result);
    }

    // ---------------------------
    // Tests for Positive (int?)
    // ---------------------------
    [Fact]
    public void Positive_NullableInt_ShouldReturnNull_WhenNoValue()
    {
        // Arrange
        int? value = null;

        // Act
        var result = value.Positive();

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(-5, 1, 1)] // negative becomes default
    [InlineData(-5, 99, 99)] // negative becomes custom default
    [InlineData(0, 1, 1)] // zero becomes default
    [InlineData(10, 1, 10)] // positive stays same
    public void Positive_NullableInt_ShouldReturnPositiveValue(int? value, int toDefault, int expected)
    {
        // Act
        var result = value.Positive(toDefault);

        // Assert
        Assert.True(result > 0);
        Assert.Equal(expected, result);
    }
}
