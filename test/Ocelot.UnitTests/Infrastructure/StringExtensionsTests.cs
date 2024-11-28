using Ocelot.Infrastructure.Extensions;

namespace Ocelot.UnitTests.Infrastructure;

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
}
