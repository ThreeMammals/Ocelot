using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Metadata;
using Ocelot.Values;
using System.Text.Json;

namespace Ocelot.UnitTests.Configuration;

[Trait("Feat", "738")]
public class DownstreamRouteExtensionsTests
{
    private readonly DownstreamRoute _downstreamRoute;

    public DownstreamRouteExtensionsTests()
    {
        _downstreamRoute = new DownstreamRoute(
            null,
            new UpstreamPathTemplate(null, 0, false, null),
            new List<HeaderFindAndReplace>(),
            new List<HeaderFindAndReplace>(),
            new List<DownstreamHostAndPort>(),
            null,
            null,
            new HttpHandlerOptions(false, false, false, false, 0, TimeSpan.Zero),
            default,
            default,
            new QoSOptions(0, 0, 0, null),
            null,
            null,
            default,
            new CacheOptions(0, null, null, null),
            new LoadBalancerOptions(null, null, 0),
            new RateLimitOptions(false, null, null, false, null, null, null, 0),
            new Dictionary<string, string>(),
            new List<ClaimToThing>(),
            new List<ClaimToThing>(),
            new List<ClaimToThing>(),
            new List<ClaimToThing>(),
            default,
            default,
            new AuthenticationOptions(null, null, null),
            new DownstreamPathTemplate(null),
            null,
            new List<string>(),
            new List<AddHeader>(),
            new List<AddHeader>(),
            default,
            new SecurityOptions(),
            null,
            new Version(),
            HttpVersionPolicy.RequestVersionExact,
            new(),
            new MetadataOptions(new FileMetadataOptions()),
            0);
    }

    [Theory]
    [InlineData("key1", null)]
    [InlineData("hello", "world")]
    public void Should_return_default_value_when_key_not_found(string key, string defaultValue)
    {
        // Arrange
        _downstreamRoute.MetadataOptions.Metadata.Add(key, defaultValue);

        // Act
        var metadataValue = _downstreamRoute.GetMetadata(key, defaultValue);

        // Assert
        metadataValue.ShouldBe(defaultValue);
    }

    [Theory]
    [InlineData("hello", "world")]
    [InlineData("object.key", "value1,value2,value3")]
    public void Should_return_found_metadata_value(string key, string value)
    {
        // Arrange
        _downstreamRoute.MetadataOptions.Metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadata<string>(key);

        //Assert
        metadataValue.ShouldBe(value);
    }

    [Theory]
    [InlineData("mykey", "")]
    [InlineData("mykey", "value1", "value1")]
    [InlineData("mykey", "value1,value2", "value1", "value2")]
    [InlineData("mykey", "value1, value2", "value1", "value2")]
    [InlineData("mykey", "value1,,,value2", "value1", "value2")]
    [InlineData("mykey", "value1,   ,value2", "value1", "value2")]
    [InlineData("mykey", "value1, value2, value3", "value1", "value2", "value3")]
    [InlineData("mykey", ",  ,value1,  ,,  ,,,,,value2,,,   ", "value1", "value2")]
    public void Should_split_strings(string key, string value, params string[] expected)
    {
        // Arrange
        _downstreamRoute.MetadataOptions.Metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadata<string[]>(key);

        //Assert
        metadataValue.ShouldBe(expected);
    }

    [Fact]
    public void Should_parse_from_json_null() => Should_parse_object_from_json<object>("mykey", "null", null);

    [Fact]
    public void Should_parse_from_json_string() => Should_parse_object_from_json<string>("mykey", "string", "string");

    [Fact]
    public void Should_parse_from_json_numbers() => Should_parse_object_from_json<int>("mykey", "123", 123);

    [Fact]
    public void Should_parse_from_object()
        => Should_parse_object_from_json<FakeObject>(
            "mykey",
            "{\"Id\": 88, \"Value\": \"Hello World!\", \"MyTime\": \"2024-01-01T10:10:10.000Z\"}",
            new FakeObject { Id = 88, Value = "Hello World!", MyTime = new DateTime(2024, 1, 1, 10, 10, 10, DateTimeKind.Unspecified) });

    private void Should_parse_object_from_json<T>(string key, string value, object expected)
    {
        // Arrange
        _downstreamRoute.MetadataOptions.Metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadata<T>(key);

        //Assert
        metadataValue.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void Should_parse_from_json_array()
    {
        // Arrange
        var key = "mykey";
        _downstreamRoute.MetadataOptions.Metadata.Add(key, "[\"value1\", \"value2\", \"value3\"]");

        // Act
        var metadataValue = _downstreamRoute.GetMetadata<IEnumerable<string>>(key);

        //Assert
        IEnumerable<string> enumerable = metadataValue as string[] ?? metadataValue.ToArray();
        enumerable.ShouldNotBeNull();
        enumerable.ElementAt(0).ShouldBe("value1");
        enumerable.ElementAt(1).ShouldBe("value2");
        enumerable.ElementAt(2).ShouldBe("value3");
    }

    [Fact]
    public void Should_throw_error_when_invalid_json()
    {
        // Arrange
        var key = "mykey";
        _downstreamRoute.MetadataOptions.Metadata.Add(key, "[[[");

        // Act

        //Assert
        Assert.Throws<JsonException>(() =>
        {
            _ = _downstreamRoute.GetMetadata<IEnumerable<string>>(key);
        });
    }

    [Fact]
    public void Should_parse_json_with_custom_json_settings_options()
    {
        // Arrange
        var key = "mykey";
        var value = "{\"id\": 88, \"value\": \"Hello World!\", \"myTime\": \"2024-01-01T10:10:10.000Z\"}";
        var expected = new FakeObject
        {
            Id = 88,
            Value = "Hello World!",
            MyTime = new DateTime(2024, 1, 1, 10, 10, 10, DateTimeKind.Unspecified),
        };
        var serializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        _downstreamRoute.MetadataOptions.Metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadata<FakeObject>(key, jsonSerializerOptions: serializerOptions);

        //Assert
        metadataValue.ShouldBeEquivalentTo(expected);
    }

    record FakeObject
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public DateTime MyTime { get; set; }
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("99", 99)]
    [InlineData("500", 500)]
    [InlineData("999999999", 999999999)]
    public void Should_parse_integers(string value, int expected) => Should_parse_number(value, expected);

    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.5", 0.5)]
    [InlineData("99", 99)]
    [InlineData("99.5", 99.5)]
    [InlineData("999999999", 999999999)]
    [InlineData("999999999.5", 999999999.5)]
    public void Should_parse_double(string value, double expected) => Should_parse_number(value, expected);

    private void Should_parse_number<T>(string value, T expected)
    {
        // Arrange
        var key = "mykey";
        _downstreamRoute.MetadataOptions.Metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadata<T>(key);

        //Assert
        metadataValue.ShouldBe(expected);
    }

    [Fact]
    public void Should_throw_error_when_invalid_number()
    {
        // Arrange
        var key = "mykey";
        _downstreamRoute.MetadataOptions.Metadata.Add(key, "xyz");

        // Act

        // Assert
        Assert.Throws<FormatException>(() =>
        {
            _ = _downstreamRoute.GetMetadata<int>(key);
        });
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("yes", true)]
    [InlineData("on", true)]
    [InlineData("enabled", true)]
    [InlineData("enable", true)]
    [InlineData("ok", true)]
    [InlineData("  true  ", true)]
    [InlineData("  yes  ", true)]
    [InlineData("  on  ", true)]
    [InlineData("  enabled  ", true)]
    [InlineData("  enable  ", true)]
    [InlineData("  ok  ", true)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("off", false)]
    [InlineData("disabled", false)]
    [InlineData("disable", false)]
    [InlineData("no", false)]
    [InlineData("abcxyz", false)]
    public void Should_parse_truthy_values(string value, bool expected)
    {
        // Arrange
        var key = "mykey";
        _downstreamRoute.MetadataOptions.Metadata.Add(key, value);

        // Act
        var isTrusthy = _downstreamRoute.GetMetadata<bool>(key);

        //Assert
        isTrusthy.ShouldBe(expected);
    }
}
