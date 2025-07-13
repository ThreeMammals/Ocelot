﻿using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Headers;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Logging;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Headers;

public class AddHeadersToRequestClaimToThingTests : UnitTest
{
    private readonly AddHeadersToRequest _addHeadersToRequest;
    private readonly Mock<IClaimsParser> _parser;
    private readonly DownstreamRequest _downstreamRequest;
    private readonly Mock<IPlaceholders> _placeholders;
    private readonly Mock<IOcelotLoggerFactory> _factory;

    public AddHeadersToRequestClaimToThingTests()
    {
        _parser = new Mock<IClaimsParser>();
        _placeholders = new Mock<IPlaceholders>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _addHeadersToRequest = new AddHeadersToRequest(_parser.Object, _placeholders.Object, _factory.Object);
        _downstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com"));
    }

    [Fact]
    public void Should_add_headers_to_downstreamRequest()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        var configuration = new List<ClaimToThing>
        {
            new("header-key", string.Empty, string.Empty, 0),
        };
        var claimValue = GivenTheClaimParserReturns(new OkResponse<string>("value"));

        // Act
        var result = _addHeadersToRequest.SetHeadersOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeFalse();
        ThenTheHeaderIsAdded(claimValue);
    }

    [Fact]
    public void Should_replace_existing_headers_on_request()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("test", "data"),
        };
        var configuration = new List<ClaimToThing>
        {
            new("header-key", string.Empty, string.Empty, 0),
        };
        var claimValue = GivenTheClaimParserReturns(new OkResponse<string>("value"));
        _downstreamRequest.Headers.Add("header-key", "initial");

        // Act
        var result = _addHeadersToRequest.SetHeadersOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeFalse();
        ThenTheHeaderIsAdded(claimValue);
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
        _ = GivenTheClaimParserReturns(new ErrorResponse<string>(new List<Error> { new AnyError() }));

        // Act
        var result = _addHeadersToRequest.SetHeadersOnDownstreamRequest(configuration, claims, _downstreamRequest);

        // Assert
        result.IsError.ShouldBeTrue();
    }

    private Response<string> GivenTheClaimParserReturns(Response<string> claimValue)
    {
        _parser.Setup(x => x.GetValue(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(claimValue);
        return claimValue;
    }

    private void ThenTheHeaderIsAdded(Response<string> claimValue)
        => _downstreamRequest.Headers.First(x => x.Key == "header-key").Value.First().ShouldBe(claimValue.Data);

    private class AnyError : Error
    {
        public AnyError()
            : base("blahh", OcelotErrorCode.UnknownError, 404)
        {
        }
    }
}
