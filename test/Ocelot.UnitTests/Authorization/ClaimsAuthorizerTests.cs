using Ocelot.Authorization;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Responses;
using System.Security.Claims;

namespace Ocelot.UnitTests.Authorization;

public class ClaimsAuthorizerTests : UnitTest
{
    private readonly ClaimsAuthorizer _claimsAuthorizer;
    private ClaimsPrincipal _claimsPrincipal;
    private Dictionary<string, string> _requirement;
    private List<PlaceholderNameAndValue> _urlPathPlaceholderNameAndValues;
    private Response<bool> _result;

    public ClaimsAuthorizerTests()
    {
        _claimsAuthorizer = new ClaimsAuthorizer(new ClaimsParser());
    }

    [Fact]
    public void Should_authorize_user()
    {
        // Arrange
        GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new("UserType", "registered"),
        })));
        GivenARouteClaimsRequirement(new Dictionary<string, string>
        {
            {"UserType", "registered"},
        });

        // Act
        WhenICallTheAuthorizer();

        // Assert
        ThenTheUserIsAuthorized();
    }

    [Fact]
    public void Should_authorize_dynamic_user()
    {
        // Arrange
        GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new("userid", "14"),
        })));
        GivenARouteClaimsRequirement(new Dictionary<string, string>
        {
            {"userid", "{userId}"},
        });
        GivenAPlaceHolderNameAndValueList(new List<PlaceholderNameAndValue>
        {
            new("{userId}", "14"),
        });

        // Act
        WhenICallTheAuthorizer();

        // Assert
        ThenTheUserIsAuthorized();
    }

    [Fact]
    public void Should_not_authorize_dynamic_user()
    {
        // Arrange
        GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new("userid", "15"),
        })));
        GivenARouteClaimsRequirement(new Dictionary<string, string>
        {
            {"userid", "{userId}"},
        });
        GivenAPlaceHolderNameAndValueList(new List<PlaceholderNameAndValue>
        {
            new("{userId}", "14"),
        });

        // Act
        WhenICallTheAuthorizer();

        // Assert
        ThenTheUserIsntAuthorized();
    }

    [Fact]
    public void Should_authorize_user_multiple_claims_of_same_type()
    {
        // Arrange
        GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
        {
            new("UserType", "guest"),
            new("UserType", "registered"),
        })));
        GivenARouteClaimsRequirement(new Dictionary<string, string>
        {
            {"UserType", "registered"},
        });

        // Act
        WhenICallTheAuthorizer();

        // Assert
        ThenTheUserIsAuthorized();
    }

    [Fact]
    public void Should_not_authorize_user()
    {
        // Arrange
        GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>())));
        GivenARouteClaimsRequirement(new Dictionary<string, string>
        {
            { "UserType", "registered" },
        });

        // Act
        WhenICallTheAuthorizer();

        // Assert
        ThenTheUserIsntAuthorized();
    }

    private void GivenAClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
    {
        _claimsPrincipal = claimsPrincipal;
    }

    private void GivenARouteClaimsRequirement(Dictionary<string, string> requirement)
    {
        _requirement = requirement;
    }

    private void GivenAPlaceHolderNameAndValueList(List<PlaceholderNameAndValue> urlPathPlaceholderNameAndValues)
    {
        _urlPathPlaceholderNameAndValues = urlPathPlaceholderNameAndValues;
    }

    private void WhenICallTheAuthorizer()
    {
        _result = _claimsAuthorizer.Authorize(_claimsPrincipal, _requirement, _urlPathPlaceholderNameAndValues);
    }

    private void ThenTheUserIsAuthorized()
    {
        _result.Data.ShouldBe(true);
    }

    private void ThenTheUserIsntAuthorized()
    {
        _result.Data.ShouldBe(false);
    }
}
