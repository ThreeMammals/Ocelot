using Ocelot.Authorization;
using Ocelot.Errors;
using Ocelot.Infrastructure.Claims.Parser;
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

    [Fact]
    public void Should_split_space_separated_scope_and_match()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "api.read", "api.write" };
        var userScopes = new List<string> { "api.read api.write openid" }; // Space-separated scope claim
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_split_space_separated_scope_and_match_single_scope()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "api.write" };
        var userScopes = new List<string> { "api.read api.write openid" }; // Space-separated scope claim
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_split_space_separated_scope_but_not_match()
    {
        // Arrange
        var fakeError = new FakeError();
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "admin" };
        var userScopes = new List<string> { "api.read api.write openid" }; // Space-separated scope claim
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new ErrorResponse<bool>(fakeError));
    }

    [Fact]
    public void Should_handle_multiple_scope_claims_without_splitting()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "api.read" };
        var userScopes = new List<string> { "api.read", "api.write" }; // Multiple separate claims
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_not_split_single_scope_without_spaces()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "api.read" };
        var userScopes = new List<string> { "api.read" }; // Single scope without spaces
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_handle_empty_string_after_splitting()
    {
        // Arrange
        var principal = new ClaimsPrincipal();
        var allowedScopes = new List<string> { "api.read" };
        var userScopes = new List<string> { "  api.read  api.write  " }; // Scope with extra spaces
        GivenTheParserReturns(new OkResponse<List<string>>(userScopes));

        // Act
        var result = _authorizer.Authorize(principal, allowedScopes);

        // Assert
        ThenTheFollowingIsReturned(result, new OkResponse<bool>(true));
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
