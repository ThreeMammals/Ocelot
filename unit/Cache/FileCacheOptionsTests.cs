using Ocelot.Configuration.File;

namespace Ocelot.UnitTests.Cache;

public class FileCacheOptionsTests
{
    [Fact]
    public void Ctor_int()
    {
        var actual = new FileCacheOptions(3);
        Assert.Equal(3, actual.TtlSeconds);
        Assert.Null(actual.Region);
        Assert.Null(actual.Header);
        Assert.Null(actual.EnableContentHashing);
    }

    [Fact]
    public void Ctor_FileCacheOptions()
    {
        FileCacheOptions from = new()
        {
            TtlSeconds = 4,
            Region = "region",
            Header = "header",
            EnableContentHashing = true,
        };
        FileCacheOptions actual = new(from);

        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
        Assert.Equal(4, actual.TtlSeconds);
    }
}
