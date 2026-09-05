using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Claims;
using Ocelot.QueryStrings;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.QueryStrings;

/// <summary>
/// Feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/claimstransformation.rst#claims-to-query-string-parameters">Claims to Query String Parameters</see>.
/// </summary>
[Trait("Commit", "f7f4a39")] // https://github.com/ThreeMammals/Ocelot/commit/f7f4a392f0743b38cd0206a81b4c094e60fe7b93
[Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
public class AddQueriesToRequestTests : UnitTest
{
    private readonly AddQueriesToRequest _addQueriesToRequest;
    private DownstreamRequest _downstreamRequest;
    private readonly Mock<IClaimsParser> _parser;
    private HttpRequestMessage _request;

    public AddQueriesToRequestTests()
    {
        _request = new HttpRequestMessage(HttpMethod.Post, "http://my.url/abc?q=123");
        _parser = new Mock<IClaimsParser>();
        _addQueriesToRequest = new AddQueriesToRequest(_parser.Object);
        _downstreamRequest = new DownstreamRequest(_request);
    }

    [Fact]
    public void Should_add_new_queries_to_downstream_request()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        var configuration = new List<ClaimToThing>
        {
            new("query-key", string.Empty, string.Empty, 0),
        };
        var claimValue = GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        var result = _addQueriesToRequest.SetQueriesOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeFalse();
        ThenTheQueryIsAdded(claimValue);
    }

    [Fact]
    public void Should_add_new_queries_to_downstream_request_and_preserve_other_queries()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        var configuration = new List<ClaimToThing>
        {
            new("query-key", string.Empty, string.Empty, 0),
        };
        GivenTheDownstreamRequestHasQueryString("?test=1&test=2");
        var claimValue = GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        var result = _addQueriesToRequest.SetQueriesOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeFalse();
        ThenTheQueryIsAdded(claimValue);
        TheTheQueryStringIs("?test=1&test=2&query-key=value");
    }

    private void TheTheQueryStringIs(string expected)
    {
        _downstreamRequest.Query.ShouldBe(expected);
    }

    [Fact]
    public void Should_replace_existing_queries_on_downstream_request()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        var configuration = new List<ClaimToThing>
        {
            new("query-key", string.Empty, string.Empty, 0),
        };
        GivenTheDownstreamRequestHasQueryString("query-key", "initial");
        var claimValue = GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        var result = _addQueriesToRequest.SetQueriesOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeFalse();
        ThenTheQueryIsAdded(claimValue);
    }

    [Fact]
    public void Should_return_error()
    {
        // Arrange
        var claims = new List<Claim>();
        var configuration = new List<ClaimToThing>
        {
            new(string.Empty, string.Empty, string.Empty, 0),
        };
        _ = GivenTheClaimParserReturns(new ErrorResponse<string>(new List<Error>
        {
            new AnyError(),
        }));

        // Act
        var result = _addQueriesToRequest.SetQueriesOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    private void ThenTheQueryIsAdded(Response<string> claimValue)
    {
        var queries = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(_downstreamRequest.ToHttpRequestMessage().RequestUri.OriginalString);
        var query = queries.First(x => x.Key == "query-key");
        query.Value.First().ShouldBe(claimValue.Data);
    }

    private void GivenTheDownstreamRequestHasQueryString(string queryString)
    {
        _request = new HttpRequestMessage(HttpMethod.Post, $"http://my.url/abc{queryString}");
        _downstreamRequest = new DownstreamRequest(_request);
    }

    private void GivenTheDownstreamRequestHasQueryString(string key, string value)
    {
        var newUri = Microsoft.AspNetCore.WebUtilities.QueryHelpers
            .AddQueryString(_downstreamRequest.ToHttpRequestMessage().RequestUri.OriginalString, key, value);

        _request.RequestUri = new Uri(newUri);
    }

    private Response<string> GivenTheClaimParserReturns(Response<string> claimValue)
    {
        _parser.Setup(x => x.GetValue(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(claimValue);
        return claimValue;
    }

    private class AnyError : Error
    {
        public AnyError()
            : base("blahh", OcelotErrorCode.UnknownError, 404)
        {
        }
    }
}
