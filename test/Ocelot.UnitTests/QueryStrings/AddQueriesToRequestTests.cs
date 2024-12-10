﻿using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.QueryStrings;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.QueryStrings;

public class AddQueriesToRequestTests : UnitTest
{
    private readonly AddQueriesToRequest _addQueriesToRequest;
    private DownstreamRequest _downstreamRequest;
    private readonly Mock<IClaimsParser> _parser;
    private List<ClaimToThing> _configuration;
    private List<Claim> _claims;
    private Response _result;
    private Response<string> _claimValue;
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
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        GivenAClaimToThing(new List<ClaimToThing>
            {
                new("query-key", string.Empty, string.Empty, 0),
            });
        GivenClaims(claims);
        GivenTheClaimParserReturns(new OkResponse<string>("value"));
        WhenIAddQueriesToTheRequest();
        ThenTheResultIsSuccess();
        ThenTheQueryIsAdded();
    }

    [Fact]
    public void Should_add_new_queries_to_downstream_request_and_preserve_other_queries()
    {
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        GivenAClaimToThing(new List<ClaimToThing>
            {
                new("query-key", string.Empty, string.Empty, 0),
            });
        GivenClaims(claims);
        GivenTheDownstreamRequestHasQueryString("?test=1&test=2");
        GivenTheClaimParserReturns(new OkResponse<string>("value"));
        WhenIAddQueriesToTheRequest();
        ThenTheResultIsSuccess();
        ThenTheQueryIsAdded();
        TheTheQueryStringIs("?test=1&test=2&query-key=value");
    }

    private void TheTheQueryStringIs(string expected)
    {
        _downstreamRequest.Query.ShouldBe(expected);
    }

    [Fact]
    public void Should_replace_existing_queries_on_downstream_request()
    {
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        GivenAClaimToThing(new List<ClaimToThing>
            {
                new("query-key", string.Empty, string.Empty, 0),
            });
        GivenClaims(claims);
        GivenTheDownstreamRequestHasQueryString("query-key", "initial");
        GivenTheClaimParserReturns(new OkResponse<string>("value"));
        WhenIAddQueriesToTheRequest();
        ThenTheResultIsSuccess();
        ThenTheQueryIsAdded();
    }

    [Fact]
    public void Should_return_error()
    {
        GivenAClaimToThing(new List<ClaimToThing>
           {
                new(string.Empty, string.Empty, string.Empty, 0),
           });
        GivenClaims(new List<Claim>());
        GivenTheClaimParserReturns(new ErrorResponse<string>(new List<Error>
           {
               new AnyError(),
           }));
        WhenIAddQueriesToTheRequest();
        ThenTheResultIsError();
    }

    private void ThenTheQueryIsAdded()
    {
        var queries = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(_downstreamRequest.ToHttpRequestMessage().RequestUri.OriginalString);
        var query = queries.First(x => x.Key == "query-key");
        query.Value.First().ShouldBe(_claimValue.Data);
    }

    private void GivenAClaimToThing(List<ClaimToThing> configuration)
    {
        _configuration = configuration;
    }

    private void GivenClaims(List<Claim> claims)
    {
        _claims = claims;
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

    private void GivenTheClaimParserReturns(Response<string> claimValue)
    {
        _claimValue = claimValue;
        _parser
            .Setup(
                x =>
                    x.GetValue(It.IsAny<IEnumerable<Claim>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>()))
            .Returns(_claimValue);
    }

    private void WhenIAddQueriesToTheRequest()
    {
        _result = _addQueriesToRequest.SetQueriesOnDownstreamRequest(_configuration, _claims, _downstreamRequest);
    }

    private void ThenTheResultIsSuccess()
    {
        _result.IsError.ShouldBe(false);
    }

    private void ThenTheResultIsError()
    {
        _result.IsError.ShouldBe(true);
    }

    private class AnyError : Error
    {
        public AnyError()
            : base("blahh", OcelotErrorCode.UnknownError, 404)
        {
        }
    }
}
