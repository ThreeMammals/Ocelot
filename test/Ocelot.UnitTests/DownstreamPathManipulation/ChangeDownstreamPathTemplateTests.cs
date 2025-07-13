using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.PathManipulation;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;
using Ocelot.Values;
using System.Security.Claims;

namespace Ocelot.UnitTests.DownstreamPathManipulation;

public class ChangeDownstreamPathTemplateTests : UnitTest
{
    private readonly ChangeDownstreamPathTemplate _changeDownstreamPath;
    private DownstreamPathTemplate _downstreamPathTemplate;
    private readonly Mock<IClaimsParser> _parser;
    private List<ClaimToThing> _configuration;
    private List<Claim> _claims;
    private Response _result;
    private Response<string> _claimValue;
    private List<PlaceholderNameAndValue> _placeholderValues;

    public ChangeDownstreamPathTemplateTests()
    {
        _parser = new Mock<IClaimsParser>();
        _changeDownstreamPath = new ChangeDownstreamPathTemplate(_parser.Object);
    }

    [Fact]
    public void Should_change_downstream_path_request()
    {
        // Arrange
        _claims = new List<Claim>
        {
            new("test", "data"),
        };
        _placeholderValues = new List<PlaceholderNameAndValue>();
        _configuration = new List<ClaimToThing>
        {
            new("path-key", string.Empty, string.Empty, 0),
        };
        _downstreamPathTemplate = new DownstreamPathTemplate("/api/test/{path-key}");
        GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        WhenIChangeDownstreamPath();

        // Assert
        _result.IsError.ShouldBeFalse();
        ThenClaimDataIsContainedInPlaceHolder("{path-key}", "value");
    }

    [Fact]
    public void Should_replace_existing_placeholder_value()
    {
        // Arrange
        _claims = new List<Claim>
        {
            new("test", "data"),
        };
        _placeholderValues = new List<PlaceholderNameAndValue>
        {
            new("{path-key}", "old_value"),
        };
        _configuration = new List<ClaimToThing>
        {
            new("path-key", string.Empty, string.Empty, 0),
        };
        _downstreamPathTemplate = new DownstreamPathTemplate("/api/test/{path-key}");
        GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        WhenIChangeDownstreamPath();

        // Assert
        _result.IsError.ShouldBeFalse();
        ThenClaimDataIsContainedInPlaceHolder("{path-key}", "value");
    }

    [Fact]
    public void Should_return_error_when_no_placeholder_in_downstream_path()
    {
        // Arrange
        _claims = new List<Claim>
        {
            new("test", "data"),
        };
        _placeholderValues = new List<PlaceholderNameAndValue>();
        _configuration = new List<ClaimToThing>
        {
            new("path-key", string.Empty, string.Empty, 0),
        };
        _downstreamPathTemplate = new DownstreamPathTemplate("/api/test");
        GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        WhenIChangeDownstreamPath();

        // Assert
        _result.IsError.ShouldBe(true);
        _result.Errors.Count.ShouldBe(1);
        _result.Errors.First().ShouldBeOfType<CouldNotFindPlaceholderError>();
    }

    [Fact]
    public void Should_return_error_when_claim_parser_returns_error()
    {
        // Arrange
        _claims = new List<Claim>
        {
            new("test", "data"),
        };
        _placeholderValues = new List<PlaceholderNameAndValue>();
        _configuration = new List<ClaimToThing>
        {
            new("path-key", string.Empty, string.Empty, 0),
        };
        _downstreamPathTemplate = new DownstreamPathTemplate("/api/test/{path-key}");
        GivenTheClaimParserReturns(new ErrorResponse<string>(new List<Error>
            {
               new AnyError(),
            }));

        // Act
        WhenIChangeDownstreamPath();

        // Assert
        _result.IsError.ShouldBe(true);
    }

    private void GivenTheClaimParserReturns(Response<string> claimValue)
    {
        _claimValue = claimValue;
        _parser.Setup(x => x.GetValue(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(_claimValue);
    }

    private void WhenIChangeDownstreamPath()
        => _result = _changeDownstreamPath.ChangeDownstreamPath(_configuration, _claims, _downstreamPathTemplate, _placeholderValues);

    private void ThenClaimDataIsContainedInPlaceHolder(string name, string value)
    {
        var placeHolder = _placeholderValues.FirstOrDefault(ph => ph.Name == name && ph.Value == value);
        placeHolder.ShouldNotBeNull();
        _placeholderValues.Count.ShouldBe(1);
    }
}
