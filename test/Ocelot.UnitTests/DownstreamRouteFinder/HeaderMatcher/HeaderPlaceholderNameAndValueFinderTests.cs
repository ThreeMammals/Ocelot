using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamRouteFinder.HeaderMatcher;

[Trait("PR", "1312")]
[Trait("Feat", "360")]
public class HeaderPlaceholderNameAndValueFinderTests : UnitTest
{
    private readonly IHeaderPlaceholderNameAndValueFinder _finder;
    private Dictionary<string, string> _upstreamHeaders;
    private Dictionary<string, UpstreamHeaderTemplate> _upstreamHeaderTemplates;
    private List<PlaceholderNameAndValue> _result;

    public HeaderPlaceholderNameAndValueFinderTests()
    {
        _finder = new HeaderPlaceholderNameAndValueFinder();
    }

    [Fact]
    public void Should_return_no_placeholders()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>();
        var upstreamHeaders = new Dictionary<string, string>();
        var expected = new List<PlaceholderNameAndValue>();
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_no_other_text()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^(?i)(?<countrycode>.+)$", "{header:countrycode}"),
        };
        var upstreamHeaders = new Dictionary<string, string>
        {
            ["country"] = "PL",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_other_text_on_the_right()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^(?<countrycode>.+)-V1$", "{header:countrycode}-V1"),
        };
        var upstreamHeaders = new Dictionary<string, string>
        {
            ["country"] = "PL-V1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_other_text_on_the_left()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^V1-(?<countrycode>.+)$", "V1-{header:countrycode}"),
        };
        var upstreamHeaders = new Dictionary<string, string>
        {
            ["country"] = "V1-PL",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
    }

    [Fact]
    public void Should_return_one_placeholder_with_value_when_other_texts_surrounding()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["country"] = new("^cc:(?<countrycode>.+)-V1$", "cc:{header:countrycode}-V1"),
        };
        var upstreamHeaders = new Dictionary<string, string>
        {
            ["country"] = "cc:PL-V1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
        };
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
    }

    [Fact]
    public void Should_return_two_placeholders_with_text_between()
    {
        // Arrange
        var upstreamHeaderTemplates = new Dictionary<string, UpstreamHeaderTemplate>
        {
            ["countryAndVersion"] = new("^(?i)(?<countrycode>.+)-(?<version>.+)$", "{header:countrycode}-{header:version}"),
        };
        var upstreamHeaders = new Dictionary<string, string>
        {
            ["countryAndVersion"] = "PL-v1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
            new("{version}", "v1"),
        };
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
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
        var upstreamHeaders = new Dictionary<string, string>
        {
            ["country"] = "PL",
            ["version"] = "v1",
        };
        var expected = new List<PlaceholderNameAndValue>
        {
            new("{countrycode}", "PL"),
            new("{version}", "v1"),
        };
        GivenUpstreamHeaderTemplatesAre(upstreamHeaderTemplates);
        GivenUpstreamHeadersAre(upstreamHeaders);

        // Act
        WhenICallFindPlaceholders();

        // Assert
        TheResultIs(expected);
    }

    private void GivenUpstreamHeaderTemplatesAre(Dictionary<string, UpstreamHeaderTemplate> upstreaHeaderTemplates)
    {
        _upstreamHeaderTemplates = upstreaHeaderTemplates;
    }

    private void GivenUpstreamHeadersAre(Dictionary<string, string> upstreamHeaders)
    {
        _upstreamHeaders = upstreamHeaders;
    }

    private void WhenICallFindPlaceholders()
    {
        var result = _finder.Find(_upstreamHeaders, _upstreamHeaderTemplates);
        _result = new(result);
    }

    private void TheResultIs(List<PlaceholderNameAndValue> expected)
    {
        _result.ShouldNotBeNull();
        _result.Count.ShouldBe(expected.Count);
        _result.ForEach(x => expected.Any(e => e.Name == x.Name && e.Value == x.Value).ShouldBeTrue());
    }
}
