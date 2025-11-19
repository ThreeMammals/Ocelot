using Ocelot.Configuration;

namespace Ocelot.UnitTests.Configuration;

public class HeaderFindAndReplaceTests
{
    [Fact]
    [Trait("PR", "1659")]
    public void Ctor_Copying_Copied()
    {
        // Arrange
        HeaderFindAndReplace expected = new("1", "2", "3", 4);

        // Act
        HeaderFindAndReplace actual = new(expected); // copying

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    [Fact]
    [Trait("PR", "1659")]
    public void Ctor_KeyValuePair_Copied()
    {
        // Arrange
        KeyValuePair<string, string> from = new("Location", "XXX, YYY");
        HeaderFindAndReplace expected = new("Location", "XXX", "YYY", 0);

        // Act
        HeaderFindAndReplace actual = new(from); // copying

        // Assert
        Assert.Equivalent(expected, actual);
        AssertEquality(actual, expected);
    }

    private const string Em = "";

    [Theory]
    [Trait("PR", "1659")]
    [InlineData(null, Em, Em)]
    [InlineData("", Em, Em)]
    [InlineData(" ", Em, Em)]
    [InlineData("x", "x", Em)]
    [InlineData("x,y", "x", "y")]
    public void Ctor_KeyValuePair_ArgIsChecked(string value, string find, string replace)
    {
        // Arrange
        KeyValuePair<string, string> from = new("key", value);

        // Act
        HeaderFindAndReplace actual = new(from);

        // Assert
        Assert.Equal(0, actual.Index);
        Assert.Equal("key", actual.Key);
        Assert.Equal(find, actual.Find);
        Assert.Equal(replace, actual.Replace);
    }

    [Fact]
    [Trait("PR", "1659")]
    public void ToString_Serialized()
    {
        // Arrange
        HeaderFindAndReplace headerFR = new("Location", "XXX", "YYY", 3);

        // Act
        var actual = headerFR.ToString();

        // Assert
        Assert.Equal("HeaderFindAndReplace[Location at 3: XXX -> YYY]", actual);
    }

    private static void AssertEquality(HeaderFindAndReplace actual, HeaderFindAndReplace expected)
    {
        Assert.Equal(expected.Index, actual.Index);
        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.Find, actual.Find);
        Assert.Equal(expected.Replace, actual.Replace);
    }
}
