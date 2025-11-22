using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Filter;
using System.Text.Json;

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
        Assert.Null(actual.StatusCodeFilter);
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
            StatusCodeFilter = new HttpStatusCodeFilter(FilterType.Blacklist, new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }),
        };
        FileCacheOptions actual = new(from);

        Assert.False(ReferenceEquals(from, actual));
        Assert.Equivalent(from, actual);
        Assert.Equal(4, actual.TtlSeconds);
        Assert.Equal(FilterType.Blacklist, actual.StatusCodeFilter.FilterType);
        Assert.Equal(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden }, actual.StatusCodeFilter.Values);
    }


    [Fact]
    public void Serialization() // just make sure the type can be serialized and deserialized accurately.
    {
        var statusCodeFilter = new HttpStatusCodeFilter(FilterType.Blacklist, new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Forbidden });
        var options = new FileCacheOptions
        {
            TtlSeconds = 100,
            Region = "some_region",
            Header = "some_header",
            EnableContentHashing = true,
            StatusCodeFilter = statusCodeFilter,
        };
        string json = JsonSerializer.Serialize(options);
        var deserialized = JsonSerializer.Deserialize<FileCacheOptions>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(options.TtlSeconds, deserialized.TtlSeconds);
        Assert.Equal(options.Region, deserialized.Region);
        Assert.Equal(options.Header, deserialized.Header);
        Assert.Equal(options.EnableContentHashing, deserialized.EnableContentHashing);
        Assert.NotNull(options.StatusCodeFilter);
        Assert.Equal(options.StatusCodeFilter.FilterType, deserialized.StatusCodeFilter.FilterType);
        Assert.Equal(options.StatusCodeFilter.Values, deserialized.StatusCodeFilter.Values);
    }
}
