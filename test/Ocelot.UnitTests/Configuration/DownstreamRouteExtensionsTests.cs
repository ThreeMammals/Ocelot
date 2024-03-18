﻿using Ocelot.Configuration;
using Ocelot.Configuration.Creator;
using Ocelot.Values;
using System.Numerics;
using System.Text.Json;

namespace Ocelot.UnitTests.Configuration;

public class DownstreamRouteExtensionsTests
{
    private readonly Dictionary<string, string> _metadata;
    private readonly DownstreamRoute _downstreamRoute;

    public DownstreamRouteExtensionsTests()
    {
        _metadata = new Dictionary<string, string>();
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
            new CacheOptions(0, null, null),
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
            _metadata);
    }

    [Theory]
    [InlineData("key1", null)]
    [InlineData("hello", "world")]
    public void should_return_default_value_when_key_not_found(string key, string defaultValue)
    {
        // Arrange
        _metadata.Add(key, defaultValue);

        // Act
        var metadataValue = _downstreamRoute.GetMetadataValue(key, defaultValue);

        // Assert
        metadataValue.ShouldBe(defaultValue);
    }

    [Theory]
    [InlineData("hello", "world")]
    [InlineData("object.key", "value1,value2,value3")]
    public void should_return_found_metadata_value(string key, string value)
    {
        // Arrange
        _metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadataValue(key);

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
    public void should_split_strings(string key, string value, params string[] expected)
    {
        // Arrange
        _metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadataValues(key);

        //Assert
        metadataValue.ShouldBe(expected);
    }

    [Fact]
    public void should_parse_from_json_null() => should_parse_object_from_json<object>("mykey", "null", null);

    [Fact]
    public void should_parse_from_json_string() => should_parse_object_from_json<string>("mykey", "\"string\"", "string");

    [Fact]
    public void should_parse_from_json_numbers() => should_parse_object_from_json<int>("mykey", "123", 123);

    [Fact]
    public void should_parse_from_object()
        => should_parse_object_from_json<FakeObject>(
            "mykey",
            "{\"Id\": 88, \"Value\": \"Hello World!\", \"MyTime\": \"2024-01-01T10:10:10.000Z\"}",
            new FakeObject { Id = 88, Value = "Hello World!", MyTime = new DateTime(2024, 1, 1, 10, 10, 10, DateTimeKind.Unspecified) });

    private void should_parse_object_from_json<T>(string key, string value, object expected)
    {
        // Arrange
        _metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadataFromJson<T>(key);

        //Assert
        metadataValue.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void should_parse_from_json_array()
    {
        // Arrange
        var key = "mykey";
        _metadata.Add(key, "[\"value1\", \"value2\", \"value3\"]");

        // Act
        var metadataValue = _downstreamRoute.GetMetadataFromJson<IEnumerable<string>>(key);

        //Assert
        metadataValue.ShouldNotBeNull();
        metadataValue.ElementAt(0).ShouldBe("value1");
        metadataValue.ElementAt(1).ShouldBe("value2");
        metadataValue.ElementAt(2).ShouldBe("value3");
    }

    [Fact]
    public void should_throw_error_when_invalid_json()
    {
        // Arrange
        var key = "mykey";
        _metadata.Add(key, "[[[");

        // Act

        //Assert
        Assert.Throws<JsonException>(() =>
        {
            _ = _downstreamRoute.GetMetadataFromJson<IEnumerable<string>>(key);
        });
    }

    [Fact]
    public void should_parse_json_with_custom_json_settings_options()
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
        _metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadataFromJson<FakeObject>(key, jsonSerializerOptions: serializerOptions);

        //Assert
        metadataValue.ShouldBeEquivalentTo(expected);
    }

    record FakeObject
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public DateTime MyTime { get; set; }
    }

#if NET7_0_OR_GREATER

    [Theory]
    [InlineData("0", 0)]
    [InlineData("99", 99)]
    [InlineData("500", 500)]
    [InlineData("999999999", 999999999)]
    public void should_parse_integers(string value, int expected) => should_parse_number(value, expected);

    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.5", 0.5)]
    [InlineData("99", 99)]
    [InlineData("99.5", 99.5)]
    [InlineData("999999999", 999999999)]
    [InlineData("999999999.5", 999999999.5)]
    public void should_parse_double(string value, double expected) => should_parse_number(value, expected);

    private void should_parse_number<T>(string value, T expected)
        where T : INumberBase<T>
    {
        // Arrange
        var key = "mykey";
        _metadata.Add(key, value);

        // Act
        var metadataValue = _downstreamRoute.GetMetadataNumber<T>(key);

        //Assert
        metadataValue.ShouldBe(expected);
    }

    [Fact]
    public void should_throw_error_when_invalid_number()
    {

        // Arrange
        var key = "mykey";
        _metadata.Add(key, "xyz");

        // Act

        // Assert
        Assert.Throws<FormatException>(() =>
        {
            _ = _downstreamRoute.GetMetadataNumber<int>(key);
        });
    }

#endif
}
