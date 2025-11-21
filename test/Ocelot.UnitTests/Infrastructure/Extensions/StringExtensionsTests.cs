using Ocelot.Infrastructure.Extensions;

namespace Ocelot.UnitTests.Infrastructure.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void TrimPrefix_ArgsCheck_ReturnedSource()
    {
        ((string)null).TrimPrefix("/").ShouldBeNull();
        "x".TrimPrefix(null).ShouldBe("x");
        "x".TrimPrefix(string.Empty).ShouldBe("x");
    }

    [Fact]
    public void TrimPrefix_HasPrefix_HappyPath()
    {
        "/string".TrimPrefix("/").ShouldBe("string");
        "///string".TrimPrefix("/").ShouldBe("string");
        "ABABstring".TrimPrefix("AB").ShouldBe("string");
    }

    [Fact]
    public void LastCharAsForwardSlash_HappyPath()
    {
        "string".LastCharAsForwardSlash().ShouldBe("string/");
        "string/".LastCharAsForwardSlash().ShouldBe("string/");
    }

    [Theory]
    [InlineData(0, "s")]
    [InlineData(1, "")]
    [InlineData(2, "s")]
    public void Plural_Int32(int count, string expected)
    {
        var actual = count.Plural();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("item", 0, "items")]
    [InlineData("item", 1, "item")]
    [InlineData("item", 2, "items")]
    public void Plural_ThisString(string source, int count, string expected)
    {
        var actual = source.Plural(count);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("x", false)]
    public void IsEmpty(string str, bool expected)
    {
        bool actual = str.IsEmpty();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("x", true)]
    public void IsNotEmpty(string str, bool expected)
    {
        bool actual = str.IsNotEmpty();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, "def", "def")]
    [InlineData("", "def", "def")]
    [InlineData(" ", "def", "def")]
    [InlineData("x", "def", "x")]
    public void IfEmpty(string str, string def, string expected)
    {
        var actual = str.IfEmpty(def);
        Assert.Equal(expected, actual);
    }
}
