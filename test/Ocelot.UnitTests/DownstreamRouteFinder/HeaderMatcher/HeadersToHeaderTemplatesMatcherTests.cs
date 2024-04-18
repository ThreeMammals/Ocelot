using Ocelot.DownstreamRouteFinder.HeaderMatcher;
using Ocelot.Values;

namespace Ocelot.UnitTests.DownstreamRouteFinder.HeaderMatcher;

[Trait("PR", "1312")]
[Trait("Feat", "360")]
public class HeadersToHeaderTemplatesMatcherTests : UnitTest
{
    private readonly IHeadersToHeaderTemplatesMatcher _headerMatcher;
    private Dictionary<string, string> _upstreamHeaders;
    private Dictionary<string, UpstreamHeaderTemplate> _templateHeaders;
    private bool _result;

    public HeadersToHeaderTemplatesMatcherTests()
    {
        _headerMatcher = new HeadersToHeaderTemplatesMatcher();
    }

    [Fact]
    public void Should_match_when_no_template_headers()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "anyHeaderValue",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>();
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsTrue();
    }

    [Fact]
    public void Should_match_the_same_headers()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "anyHeaderValue",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsTrue();
    }

    [Fact]
    public void Should_not_match_the_same_headers_when_differ_case_and_case_sensitive()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "ANYHEADERVALUE",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsFalse();
    }

    [Fact]
    public void Should_match_the_same_headers_when_differ_case_and_case_insensitive()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "ANYHEADERVALUE",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsTrue();
    }

    [Fact]
    public void Should_not_match_different_headers_values()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "anyHeaderValueDifferent",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsFalse();
    }

    [Fact]
    public void Should_not_match_the_same_headers_names()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeaderDifferent"] = "anyHeaderValue",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsFalse();
    }

    [Fact]
    public void Should_match_all_the_same_headers()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "anyHeaderValue",
            ["notNeededHeader"] = "notNeededHeaderValue",
            ["secondHeader"] = "secondHeaderValue",
            ["thirdHeader"] = "thirdHeaderValue",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["secondHeader"] = new("^(?i)secondHeaderValue$", "secondHeaderValue"),
            ["thirdHeader"] = new("^(?i)thirdHeaderValue$", "thirdHeaderValue"),
            ["anyHeader"] = new("^(?i)anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsTrue();
    }

    [Fact]
    public void Should_not_match_the_headers_when_one_of_them_different()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "anyHeaderValue",
            ["notNeededHeader"] = "notNeededHeaderValue",
            ["secondHeader"] = "secondHeaderValueDIFFERENT",
            ["thirdHeader"] = "thirdHeaderValue",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["secondHeader"] = new("^(?i)secondHeaderValue$", "secondHeaderValue"),
            ["thirdHeader"] = new("^(?i)thirdHeaderValue$", "thirdHeaderValue"),
            ["anyHeader"] = new("^(?i)anyHeaderValue$", "anyHeaderValue"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsFalse();
    }

    [Fact]
    public void Should_match_the_header_with_placeholder()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "PL",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)(?<countrycode>.+)$", "{header:countrycode}"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsTrue();
    }

    [Fact]
    public void Should_match_the_header_with_placeholders()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "PL-V1",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)(?<countrycode>.+)-(?<version>.+)$", "{header:countrycode}-{header:version}"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsTrue();
    }

    [Fact]
    public void Should_not_match_the_header_with_placeholders()
    {
        // Arrange
        var upstreamHeaders = new Dictionary<string, string>()
        {
            ["anyHeader"] = "PL",
        };
        var templateHeaders = new Dictionary<string, UpstreamHeaderTemplate>()
        {
            ["anyHeader"] = new("^(?i)(?<countrycode>.+)-(?<version>.+)$", "{header:countrycode}-{header:version}"),
        };
        GivenIHaveUpstreamHeaders(upstreamHeaders);
        GivenIHaveTemplateHeadersInRoute(templateHeaders);

        // Act
        WhenIMatchTheHeaders();

        // Assert
        ThenTheResultIsFalse();
    }

    private void GivenIHaveUpstreamHeaders(Dictionary<string, string> upstreamHeaders)
    {
        _upstreamHeaders = upstreamHeaders;
    }

    private void GivenIHaveTemplateHeadersInRoute(Dictionary<string, UpstreamHeaderTemplate> templateHeaders)
    {
        _templateHeaders = templateHeaders;
    }

    private void WhenIMatchTheHeaders()
    {
        _result = _headerMatcher.Match(_upstreamHeaders, _templateHeaders);
    }

    private void ThenTheResultIsTrue()
    {
        _result.ShouldBeTrue();
    }

    private void ThenTheResultIsFalse()
    {
        _result.ShouldBeFalse();
    }
}
