using Microsoft.AspNetCore.Http;
using Ocelot.AcceptanceTests.Authorization;
using Ocelot.Configuration.File;
using Ocelot.Testing.Authentication;

namespace Ocelot.AcceptanceTests.Transformations;

/// <summary>
/// Feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/claimstransformation.rst#claims-to-query-string-parameters">Claims to Query String Parameters</see>.
/// </summary>
[Trait("Commit", "f7f4a39")] // https://github.com/ThreeMammals/Ocelot/commit/f7f4a392f0743b38cd0206a81b4c094e60fe7b93
[Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
public sealed class ClaimsToQueryStringForwardingTests : AuthorizationSteps
{
    private static Dictionary<string, string> GivenAddQueriesToRequest(FileRoute route)
    {
        route.AddQueriesToRequest = new()
        {
            { "CustomerId", "Claims[CustomerId] > value" },
            { "LocationId", "Claims[LocationId] > value" },
            { "UserType", $"Claims[{OcelotClaims.OcSub}] > value[0] > |" },
            { "UserId", $"Claims[{OcelotClaims.OcSub}] > value[1] > |" },
        };
        var claims = new Dictionary<string, string>()
        {
            { "CustomerId", "111" },
            { "LocationId", "222" },
        };
        return claims;
    }

    [BddfyFact]
    public void Should_return_200_OK_and_forward_claim_as_query_string()
    {
        var port = PortFinder.GetRandomPort();
        string[] scopes = ["openid", "offline_access", "api"];
        var route = GivenAuthRoute(port, scopes: scopes);
        var configuration = GivenConfiguration(route);
        var claims = GivenAddQueriesToRequest(route);
        var testName = TestName();
        this
            .Given(x => GivenThereIsExternalJwtSigningService(scopes, CancelMe))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Tom"))
            .And(x => GivenIUpdateSubClaim())
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe("CustomerId:111 LocationId:222 UserType:Should_return_200_OK_and_forward_claim_as_query_string UserId:1234567890"))
            .And(x => ThenTheQueryStringIs("?CustomerId=111&LocationId=222&UserId=1234567890&UserType=Should_return_200_OK_and_forward_claim_as_query_string"))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_200_OK_and_forward_claim_as_query_string_and_preserve_original_string()
    {
        var port = PortFinder.GetRandomPort();
        string[] scopes = ["openid", "offline_access", "api"];
        var route = GivenAuthRoute(port, scopes: scopes);
        var configuration = GivenConfiguration(route);
        var claims = GivenAddQueriesToRequest(route);
        var testName = TestName();
        this
            .Given(x => GivenThereIsExternalJwtSigningService(scopes, CancelMe))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Tom"))
            .And(x => GivenIUpdateSubClaim())
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/?test=1&test=2"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe("CustomerId:111 LocationId:222 UserType:Should_return_200_OK_and_forward_claim_as_query_string_and_preserve_original_string UserId:1234567890"))
            .And(x => ThenTheQueryStringIs("?test=1&test=2&CustomerId=111&LocationId=222&UserId=1234567890&UserType=Should_return_200_OK_and_forward_claim_as_query_string_and_preserve_original_string"))
        .BDDfy();
    }

    private string _downstreamQueryString;
    private void ThenTheQueryStringIs(string queryString)
    {
        _downstreamQueryString.ShouldBe(queryString);
    }

    private const string UserId = "1234567890";
    protected override void UpdateSubClaim(object sender, AuthenticationTokenRequestEventArgs e)
    {
        e.Request.UserId += "|" + UserId; // -> sub claim -> oc-sub claim
    }

    protected override Task MapStatus(HttpContext context)
    {
        _downstreamQueryString = context.Request.QueryString.Value;
        context.Request.Query.TryGetValue("CustomerId", out var customerId);
        context.Request.Query.TryGetValue("LocationId", out var locationId);
        context.Request.Query.TryGetValue("UserType", out var userType);
        context.Request.Query.TryGetValue("UserId", out var userId);
        MapStatus_ResponseBody = _ => $"CustomerId:{customerId} LocationId:{locationId} UserType:{userType} UserId:{userId}";
        return base.MapStatus(context);
    }
}
