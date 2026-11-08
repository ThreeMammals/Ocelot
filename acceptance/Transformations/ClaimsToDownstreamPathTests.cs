using Microsoft.AspNetCore.Http;
using Ocelot.Acceptance.Authorization;
using Ocelot.Testing.Authentication;

namespace Ocelot.Acceptance.Transformations;

/// <summary>
/// Feature: <see href="https://github.com/ThreeMammals/Ocelot/blob/develop/docs/features/claimstransformation.rst#claims-to-downstream-path">Claims to Downstream Path</see>.
/// </summary>
[Trait("Feat", "968")] // https://github.com/ThreeMammals/Ocelot/pull/968
[Trait("Release", "13.8.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/13.8.0
public sealed class ClaimsToDownstreamPathTests : AuthorizationSteps
{
    [Fact]
    public async Task Should_return_200_OK_and_change_downstream_path()
    {
        var port = PortFinder.GetRandomPort();
        string[] allowedScopes = ["openid", "offline_access", "api"];
        var route = GivenAuthRoute(port, scopes: allowedScopes);
        route.DownstreamPathTemplate = "/users/{userId}";
        route.UpstreamPathTemplate = "/users/{userId}";
        route.ChangeDownstreamPathTemplate = new()
        {
            { "userId", $"Claims[{OcelotClaims.OcSub}] > value[1] > |" },
        };
        var configuration = GivenConfiguration(route);
        var testName = TestName();
        await GivenThereIsExternalJwtSigningService(allowedScopes, CancelMe);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Victor");
        GivenIUpdateSubClaim();
        await GivenIHaveAToken(testName);
        GivenIHaveAddedATokenToMyRequest();
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/users");
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyShouldBeAsync("Hello from Victor");
        ThenTheDownstreamPathIs("/users/1234567890");
    }

    private const string UserId = "1234567890";
    protected override void UpdateSubClaim(object sender, AuthenticationTokenRequestEventArgs e)
    {
        e.Request.UserId += "|" + UserId; // -> sub claim -> oc-sub claim
    }

    private string _downstreamFinalPath;
    private void ThenTheDownstreamPathIs(string path)
    {
        _downstreamFinalPath.ShouldBe(path);
    }
    protected override Task MapStatus(HttpContext context)
    {
        _downstreamFinalPath = context.Request.Path.Value;
        return base.MapStatus(context);
    }
}
