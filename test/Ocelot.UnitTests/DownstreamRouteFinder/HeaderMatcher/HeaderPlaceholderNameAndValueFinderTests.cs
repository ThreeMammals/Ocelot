using Microsoft.AspNetCore.Http;
using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamRouteFinder.HeaderMatcher;

[Trait("PR", "1312")]
[Trait("Feat", "360")]
public class HeaderPlaceholderNameAndValueFinderTests : UnitTest
{
    private readonly HeaderPlaceholderNameAndValueFinder _finder = new();

    [Fact]
    public void Should_return_no_placeholders()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>();
        var upstreamHeaders = new HeaderDictionary();
        var expected = new List<PlaceholderNameAndValue>();

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_no_other_text()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^(?i)(?<countrycode>.+)$", "{header:countrycode}"),
        };
        var upstreamHeaders = new HeaderDictionary()
        {
            ["country"] = "PL",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_other_text_on_the_right()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^(?<countrycode>.+)-V1$", "{header:countrycode}-V1"),
        };
        var upstreamHeaders = new HeaderDictionary()
        {
            ["country"] = "PL-V1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_other_text_on_the_left()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^V1-(?<countrycode>.+)$", "V1-{header:countrycode}"),
        };
        var upstreamHeaders = new HeaderDictionary()
        {
            ["country"] = "V1-PL",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_other_texts_surrounding()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^cc:(?<countrycode>.+)-V1$", "cc:{header:countrycode}-V1"),
        };
        var upstreamHeaders = new HeaderDictionary()
        {
            ["country"] = "cc:PL-V1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    [Fact]
    public void Should_return_two_placeholders_with_text_between()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["countryAndVersion"] = new("^(?i)(?<countrycode>.+)-(?<version>.+)$", "{header:countrycode}-{header:version}"),
        };
        var upstreamHeaders = new HeaderDictionary()
        {
            ["countryAndVersion"] = "PL-v1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
            new("{version}", "v1"),
        };

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    [Fact]
    public void Should_return_placeholders_from_different_headers()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^(?i)(?<countrycode>.+)$", "{header:countrycode}"),
            ["version"] = new("^(?i)(?<version>.+)$", "{header:version}"),
        };
        var upstreamHeaders = new HeaderDictionary()
        {
            ["country"] = "PL",
            ["version"] = "v1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
            new("{version}", "v1"),
        };

        // Act
        var result = _finder.Find(upstreamHeaders, upstreamHeaderTemplates).ToList();

        // Assert
        TheResultIs(result, expected);
    }

    private static void TheResultIs(List<PlaceholderNameAndValue> actual, List<PlaceholderNameAndValue> expected)
    {
        actual.ShouldNotBeNull();
        actual.Count.ShouldBe(expected.Count);
        actual.ForEach(x => expected.Any(e => e.Name == x.Name && e.Value == x.Value).ShouldBeTrue());
    }
}
