using System.Collections.Generic;
using System.Security.Claims;
using Ocelot.Authorisation;
using Ocelot.Claims.Parser;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Authorization
{
    public class ClaimsAuthoriserTests
    {
        private readonly ClaimsAuthoriser _claimsAuthoriser;
        private ClaimsPrincipal _claimsPrincipal;
        private RouteClaimsRequirement _requirement;
        private Response<bool> _result;

        public ClaimsAuthoriserTests()
        {
            _claimsAuthoriser = new ClaimsAuthoriser(new ClaimsParser());
        }

        [Fact]
        public void should_authorise_user()
        {
            this.Given(x => x.GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim("UserType", "registered")
                }))))
                .And(x => x.GivenARouteClaimsRequirement(new RouteClaimsRequirement(new Dictionary<string, string>
                {
                    {"UserType", "registered"}
                })))
                .When(x => x.WhenICallTheAuthoriser())
                .Then(x => x.ThenTheUserIsAuthorised())
                .BDDfy();
        }

        [Fact]
        public void should_not_authorise_user()
        {
            this.Given(x => x.GivenAClaimsPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>()))))
            .And(x => x.GivenARouteClaimsRequirement(new RouteClaimsRequirement(new Dictionary<string, string>
                {
                    { "UserType", "registered" }
                })))
            .When(x => x.WhenICallTheAuthoriser())
            .Then(x => x.ThenTheUserIsntAuthorised())
            .BDDfy();
        }

        private void GivenAClaimsPrincipal(ClaimsPrincipal claimsPrincipal)
        {
            _claimsPrincipal = claimsPrincipal;
        }

        private void GivenARouteClaimsRequirement(RouteClaimsRequirement requirement)
        {
            _requirement = requirement;
        }

        private void WhenICallTheAuthoriser()
        {
            _result = _claimsAuthoriser.Authorise(_claimsPrincipal, _requirement);
        }

        private void ThenTheUserIsAuthorised()
        {
            _result.Data.ShouldBe(true);
        }

        private void ThenTheUserIsntAuthorised()
        {
            _result.Data.ShouldBe(false);
        }
    }
}
