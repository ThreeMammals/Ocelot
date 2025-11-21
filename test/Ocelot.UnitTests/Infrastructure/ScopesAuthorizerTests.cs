using Ocelot.Authorization;
using Ocelot.Errors;
using Ocelot.Infrastructure.Claims;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Infrastructure;

public class ScopesAuthorizerTests : UnitTest
{
    private readonly ScopesAuthorizer _authorizer;
    public Mock<IClaimsParser> _parser;

    public ScopesAuthorizerTests()
    {
        _parser = new Mock<IClaimsParser>();
        _authorizer = new ScopesAuthorizer(_parser.Object);
    }

    [Fact]
    public void Should_return_ok_if_no_allowed_scopes()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string>();

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_return_ok_if_null_allowed_scopes()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = (List<string>)null;

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_return_error_if_claims_parser_returns_error()
    {
        // Arrange
        var fakeError = new FakeError();
        var principal = new ClaimsPrincipal();
        GivenTheParserReturns(new ErrorResponse<List<string>>(fakeError));
        var allowedScopes = new List<string> { "doesntmatter" };

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new ErrorResponse<bool>(fakeError));
    }

    [Fact]
    public void Should_match_scopes_and_return_ok_result()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "someScope" };
        GivenTheParserReturns(new OkResponse<List<string>>(allowedScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_not_match_scopes_and_return_error_result()
    {
        // Arrange
        var fakeError = new FakeError();
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "someScope" };
        var userScopes = new List<string> { "anotherScope" };
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new ErrorResponse<bool>(fakeError));        
    }

    private void GivenTheParserReturns(Response<List<string>> response)
    {
        _parser.Setup(x => x.GetValuesByClaimType(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>())).Returns(response);
    }

    private static void ThenTheFollowingIsReturned(Response<bool> actual, Response<bool> expected)
    {
        actual.Data.ShouldBe(expected.Data);
        actual.IsError.ShouldBe(expected.IsError);
    }
}

public class FakeError : Error
{
    public FakeError() : base("fake error", OcelotErrorCode.CannotAddDataError, 404)
    {
    }
}
