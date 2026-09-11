using Ocelot.Infrastructure.Extensions;

namespace Ocelot.UnitTests.Infrastructure.Extensions;

public class IEnumerableExtensionsTests
{
    [Fact]
    public void ToHttpMethods_ShouldReturnEmptyHashSet_WhenCollectionIsNull()
    {
        // Arrange
        IEnumerable<string> collection = null;

        // Act
        var result = collection.ToHttpMethods();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ToHttpMethods_ShouldReturnEmptyHashSet_WhenCollectionIsEmpty()
    {
        // Arrange
        var collection = Enumerable.Empty<string>();

        // Act
        var result = collection.ToHttpMethods();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public void ToHttpMethods_ShouldReturnCorrectHttpMethod(string verb)
    {
        // Arrange
        var collection = new[] { verb };

        // Act
        var result = collection.ToHttpMethods();

        // Assert
        Assert.Single(result);
        Assert.Equal(verb, result.First().Method);
    }

    [Fact]
    public void ToHttpMethods_ShouldTrimWhitespace()
    {
        // Arrange
        var collection = new[] { "  GET  " };

        // Act
        var result = collection.ToHttpMethods();

        // Assert
        Assert.Single(result);
        Assert.Equal("GET", result.First().Method);
    }

    [Fact]
    public void ToHttpMethods_ShouldRemoveDuplicates()
    {
        // Arrange
        var collection = new[] { "GET", "GET", "POST" };

        // Act
        var result = collection.ToHttpMethods();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Method == "GET");
        Assert.Contains(result, m => m.Method == "POST");
    }

    [Fact]
    public void ToHttpMethods_ShouldHandleMixedVerbs()
    {
        // Arrange
        var collection = new[] { "GET", "POST", "PUT", "DELETE", "DELETE" };

        // Act
        var result = collection.ToHttpMethods();

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains(result, m => m.Method == "GET");
        Assert.Contains(result, m => m.Method == "POST");
        Assert.Contains(result, m => m.Method == "PUT");
        Assert.Contains(result, m => m.Method == "DELETE");
    }

    // ---------------------------
    // Tests for NotNull<T>
    // ---------------------------
    [Fact]
    public void NotNull_ShouldReturnEmptyEnumerable_WhenCollectionIsNull()
    {
        // Arrange
        IEnumerable<int> collection = null;

        // Act
        var result = collection.NotNull();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void NotNull_ShouldReturnSameCollection_WhenNotNull()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = collection.NotNull();

        // Assert
        Assert.Same(collection, result);
    }

    // ---------------------------
    // Tests for Csv
    // ---------------------------
    [Fact]
    public void Csv_ShouldReturnEmptyString_WhenCollectionIsNull()
    {
        // Arrange
        IEnumerable<string> values = null;

        // Act
        var result = values.Csv();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Csv_ShouldReturnEmptyString_WhenCollectionIsEmpty()
    {
        // Arrange
        var values = Enumerable.Empty<string>();

        // Act
        var result = values.Csv();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Csv_ShouldJoinValuesWithComma()
    {
        // Arrange
        var values = new[] { "one", "two", "three" };

        // Act
        var result = values.Csv();

        // Assert
        Assert.Equal("one,two,three", result);
    }

    [Fact]
    public void Csv_ShouldHandleSingleValue()
    {
        // Arrange
        var values = new[] { "only" };

        // Act
        var result = values.Csv();

        // Assert
        Assert.Equal("only", result);
    }

    [Fact]
    public void Csv_ShouldPreserveEmptyStrings()
    {
        // Arrange
        var values = new[] { "a", "", "b" };

        // Act
        var result = values.Csv();

        // Assert
        Assert.Equal("a,,b", result);
    }

    [Fact]
    public void Csv_ShouldTrimWhitespaceInsideValues()
    {
        // Arrange
        var values = new[] { " a ", " b ", "c" };

        // Act
        var result = values.Csv();

        // Assert
        // Note: Csv does not trim, so whitespace is preserved
        Assert.Equal(" a , b ,c", result);
    }
}
