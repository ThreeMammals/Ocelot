using Ocelot.Authorization;
using Ocelot.Infrastructure.Claims;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Infrastructure;

public class RoleAuthorizerTests : UnitTest
{
    private readonly RolesAuthorizer _authorizer;
    private readonly Mock<IClaimsParser> _parser;
    private ClaimsPrincipal _principal;
    private List<string> _requiredRole;
    private Response<bool> _result;

    public RoleAuthorizerTests()
    {
        _parser = new Mock<IClaimsParser>();
        _authorizer = new RolesAuthorizer(_parser.Object);
    }

    [Fact]
    public void Should_return_ok_if_no_allowed_scopes()
    {
        GivenTheFollowing(new ClaimsPrincipal());
        GivenTheFollowing(new List<string>());
        WhenIAuthorize();
        ThenTheFollowingIsReturned(new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_return_ok_if_null_allowed_scopes()
    {
        GivenTheFollowing(new ClaimsPrincipal());
        GivenTheFollowing((List<string>)null);
        WhenIAuthorize();
        ThenTheFollowingIsReturned(new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_return_error_if_claims_parser_returns_error()
    {
        var fakeError = new FakeError();
        GivenTheFollowing(new ClaimsPrincipal());
        GivenTheParserReturns(new ErrorResponse<List<string>>(fakeError));
        GivenTheFollowing(new List<string>() { "doesntmatter" });
        WhenIAuthorize();
        ThenTheFollowingIsReturned(new ErrorResponse<bool>(fakeError));
    }

    [Fact]
    public void Should_match_role_and_return_ok_result()
    {
        var claimsPrincipal = new ClaimsPrincipal();
        var requiredRole = new List<string>() { "someRole" };
        GivenTheFollowing(claimsPrincipal);
        GivenTheParserReturns(new OkResponse<List<string>>(requiredRole));
        GivenTheFollowing(requiredRole);
        WhenIAuthorize();
        ThenTheFollowingIsReturned(new OkResponse<bool>(true));
    }

    [Fact]
    public void Should_not_match_role_and_return_error_result()
    {
        var fakeError = new FakeError();
        var claimsPrincipal = new ClaimsPrincipal();
        var requiredRole = new List<string>() { "someRole" };
        var userRoles = new List<string>() { "anotherRole" };
        GivenTheFollowing(claimsPrincipal);
        GivenTheParserReturns(new OkResponse<List<string>>(userRoles));
        GivenTheFollowing(requiredRole);
        WhenIAuthorize();
        ThenTheFollowingIsReturned(new ErrorResponse<bool>(fakeError));
    }

    private void GivenTheParserReturns(Response<List<string>> response)
    {
        _parser.Setup(x => x.GetValuesByClaimType(It.IsAny<IEnumerable<Claim>>(), It.IsAny<string>())).Returns(response);
    }

    private void GivenTheFollowing(ClaimsPrincipal principal)
    {
        _principal = principal;
    }

    private void GivenTheFollowing(List<string> requiredRole)
    {
        _requiredRole = requiredRole;
    }

    private void WhenIAuthorize()
    {
        _result = _authorizer.Authorize(_principal, _requiredRole, null);
    }

    private void ThenTheFollowingIsReturned(Response<bool> expected)
    {
        _result.Data.ShouldBe(expected.Data);
        _result.IsError.ShouldBe(expected.IsError);
    }
}
