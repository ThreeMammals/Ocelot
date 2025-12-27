using Microsoft.AspNetCore.Http;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.AcceptanceTests.Authorization;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.AcceptanceTests.Transformations;

/// <summary>
/// Feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/claimstransformation.rst#claims-to-headers">Claims to Headers</see>.
/// </summary>
[Trait("Commit", "84256e7")] // https://github.com/ThreeMammals/Ocelot/commit/84256e7bac0fa2c8ceba92bd8fe64c8015a37cea
[Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
public sealed class ClaimsToHeadersForwardingTests : AuthorizationSteps
{
    [Fact]
    public void Should_return_200_OK_and_forward_claim_as_header()
    {
        var port = PortFinder.GetRandomPort();
        string[] allowedScopes = ["openid", "offline_access", "api"];
        var route = GivenAuthRoute(port, scopes: allowedScopes);
        route.AddHeadersToRequest = new()
        {
            { "CustomerId", "Claims[CustomerId] > value" },
            { "LocationId", "Claims[LocationId] > value" },
            { "UserType", $"Claims[{OcelotClaims.OcSub}] > value[0] > |" },
            { "UserId", $"Claims[{OcelotClaims.OcSub}] > value[1] > |" },
        };
        var configuration = GivenConfiguration(route);
        var claims = new Dictionary<string, string>()
        {
            { "CustomerId", "111" },
            { "LocationId", "222" },
        };
        var testName = TestName();
        this.Given(x => GivenThereIsExternalJwtSigningService(allowedScopes))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Tom"))
            .And(x => GivenIUpdateSubClaim())
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOK())
            .And(x => ThenTheResponseBodyShouldBe("Hello from Tom"))
            .And(x => ThenTheResponseHeaderIs("RequestHeaders", "CustomerId:111 LocationId:222 UserType:Should_return_200_OK_and_forward_claim_as_header UserId:1234567890"))
            .BDDfy();
    }

    private const string UserId = "1234567890";
    protected override void UpdateSubClaim(object sender, AuthenticationTokenRequestEventArgs e)
    {
        e.Request.UserId += "|" + UserId; // -> sub claim -> oc-sub claim
    }

    protected override Task MapStatus(HttpContext context)
    {
        var customerId = context.Request.Headers.GetCommaSeparatedValues("CustomerId").Csv();
        var locationId = context.Request.Headers.GetCommaSeparatedValues("LocationId").Csv();
        var userType = context.Request.Headers.GetCommaSeparatedValues("UserType").Csv();
        var userId = context.Request.Headers.GetCommaSeparatedValues("UserId").Csv();
        var responseBody = $"CustomerId:{customerId} LocationId:{locationId} UserType:{userType} UserId:{userId}";
        context.Response.Headers.Append("RequestHeaders", responseBody);
        return base.MapStatus(context);
    }
}
