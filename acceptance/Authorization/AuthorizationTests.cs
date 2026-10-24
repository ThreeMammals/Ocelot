using Ocelot.Configuration.File;
using Ocelot.Testing.Authentication;
using System.Security.Claims;

namespace Ocelot.Acceptance.Authorization;

public sealed class AuthorizationTests : AuthorizationSteps
{
    private static Dictionary<string, string> GivenRouteClaimsRequirement(FileRoute route, string claimType, string claimValue)
    {
        route.AddHeadersToRequest = new()
        {
            { "CustomerId", "Claims[CustomerId] > value" },
            { "LocationId", "Claims[LocationId] > value" },
            { "UserType", $"Claims[{OcelotClaims.OcSub}] > value[0] > |" },
            { "UserId", $"Claims[{OcelotClaims.OcSub}] > value[1] > |" },
        };
        route.AddClaimsToRequest = new()
        {
            { "CustomerId", "Claims[CustomerId] > value" },
            { "UserType", $"Claims[{OcelotClaims.OcSub}] > value[0] > |" },
            { "UserId", $"Claims[{OcelotClaims.OcSub}] > value[1] > |" },
        };
        var claims = new Dictionary<string, string>()
        {
            {"CustomerId", "111"},
            {"LocationId", "222"},
            {"UserType", "registered"},
        };
        route.RouteClaimsRequirement = new(claims) // require all custom claims
        {
            [claimType] = claimValue, // but require exact claim with the scope after claims-to-claims transformation
        };
        return claims;
    }

    [Fact]
    [Trait("Commit", "3285be3")] // https://github.com/ThreeMammals/Ocelot/commit/3285be3
    [Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
    public async Task Should_return_200_OK_authorizing_route()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        var claims = GivenRouteClaimsRequirement(route, "UserType", OcelotScopes.OcAdmin);
        var testName = TestName();

        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenIUpdateSubClaim();
        await GivenIHaveATokenWithClaims(claims, testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
    }

    [Fact]
    [Trait("Commit", "b8951c4")] // https://github.com/ThreeMammals/Ocelot/commit/b8951c4
    [Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
    public async Task Should_return_403_Forbidden_authorizing_route()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        var claims = GivenRouteClaimsRequirement(route, "UserType", OcelotScopes.OcAdmin);
        route.AddClaimsToRequest.Remove("UserType"); // given I don't transform UserType claim
        var testName = TestName();

        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenIUpdateSubClaim();
        await GivenIHaveATokenWithClaims(claims, testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
        await ThenTheResponseBodyShouldBeEmpty();
    }

    [Fact]
    [Trait("Feat", "100")] // https://github.com/ThreeMammals/Ocelot/issues/100
    [Trait("PR", "104")] // https://github.com/ThreeMammals/Ocelot/pull/104
    [Trait("Release", "1.4.5")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.5
    public async Task Should_return_200_OK_using_identity_server_with_allowed_scope()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        string[] allowedScopes = ["api", "api.readOnly", "openid", "offline_access"];
        var route = GivenAuthRoute(port, scopes: allowedScopes);
        var configuration = GivenConfiguration(route);

        await GivenThereIsExternalJwtSigningService(allowedScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveAToken("api.readOnly", null, null, null, testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
    }

    [Fact]
    [Trait("Feat", "100")] // https://github.com/ThreeMammals/Ocelot/issues/100
    [Trait("PR", "104")] // https://github.com/ThreeMammals/Ocelot/pull/104
    [Trait("Release", "1.4.5")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.5
    public async Task Should_return_403_Forbidden_using_identity_server_with_scope_not_allowed()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        string[] allowedScopes = ["api", "openid", "offline_access"];
        var route = GivenAuthRoute(port, scopes: allowedScopes);
        var configuration = GivenConfiguration(route);
        var allScopes = allowedScopes.Append("api.readOnly").ToArray();

        await GivenThereIsExternalJwtSigningService(allScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveATokenWithScope("api.readOnly", testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Bug", "240")] // https://github.com/ThreeMammals/Ocelot/issues/240
    [Trait("PR", "243")] // https://github.com/ThreeMammals/Ocelot/pull/243
    [Trait("Release", "3.1.6")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.6
    public async Task Should_fix_issue_240()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        route.RouteClaimsRequirement = new() // TODO this is dictionary which doesn't support multiple keys of the same value -> separated tests 
        {
            { nameof(ClaimTypes.Role), "User" }, // this key is Ok because it is not an URL containing proto delimiter aka '://'
        };
        var claims = new List<KeyValuePair<string, string>>()
        {
            new(nameof(ClaimTypes.Role), "AdminUser"),
            new(nameof(ClaimTypes.Role), "User"),
        };

        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveATokenWithClaims(claims, testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
    }

    [Fact]
    [Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
    [Trait("PR", "2414")] // https://github.com/ThreeMammals/Ocelot/pull/2414
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_200_OK_authorizing_route_with_url_shaped_claim_type()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        route.RouteClaimsRequirement = new()
        {
            { ClaimTypes.Role, "Admin" }, // URL-shaped claim type, e.g. http://schemas.microsoft.com/ws/2008/06/identity/claims/role
        };
        var claims = new List<KeyValuePair<string, string>>()
        {
            new(ClaimTypes.Role, "Admin"),
        };

        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Omar");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveATokenWithClaims(claims, testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Omar");
    }

    [Fact]
    [Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
    [Trait("PR", "2414")] // https://github.com/ThreeMammals/Ocelot/pull/2414
    [Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
    public async Task Should_return_403_Forbidden_when_url_shaped_claim_type_missing()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        route.RouteClaimsRequirement = new()
        {
            { ClaimTypes.Role, "Admin" },
        };
        var claims = new List<KeyValuePair<string, string>>()
        {
            new(nameof(ClaimTypes.Role), "Admin"), // wrong form: short "Role" instead of the required URL claim type
        };

        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Omar");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveATokenWithClaims(claims, testName);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
    [Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
    [Trait("Release", "24.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
    public async Task Should_return_200_OK_with_global_allowed_scopes()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AuthenticationProviderKeys = []; // no route auth!
        var configuration = GivenConfiguration(route);
        string[] globalScopes = ["api", "apiGlobal"];
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration(allowedScopes: globalScopes);

        GivenThereIsAConfiguration(configuration);
        await GivenThereIsExternalJwtSigningService(globalScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveAToken("apiGlobal");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyAsync();
    }

    #region PR 1478
    [Fact]
    [Trait("Bug", "913")] // https://github.com/ThreeMammals/Ocelot/issues/913
    [Trait("PR", "1478")] // https://github.com/ThreeMammals/Ocelot/pull/1478
    [Trait("Release", "24.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
    public async Task Should_return_200_OK_with_space_separated_scope_match()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scopes: ["api", "api.read", "api.write"]);
        var configuration = GivenConfiguration(route);
        string[] extraScopes = ["api.read", "openid", "offline_access"];

        GivenThereIsAConfiguration(configuration);
        await GivenThereIsExternalJwtSigningService(extraScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveATokenWithScope("api.read openid offline_access");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyAsync();
    }

    [Fact]
    [Trait("Bug", "913")]
    [Trait("PR", "1478")]
    [Trait("Release", "24.1.0")]
    public async Task Should_return_403_Forbidden_with_space_separated_scope_no_match()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scopes: ["admin", "superuser"]);
        var configuration = GivenConfiguration(route);
        string[] extraScopes = ["api.read", "api.write", "openid"];

        await GivenThereIsExternalJwtSigningService(extraScopes, CancelMe);
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenIHaveATokenWithScope("api.read api.write openid");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
    }
    #endregion PR 1478
}
