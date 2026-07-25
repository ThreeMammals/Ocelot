using Ocelot.Configuration.File;
using Ocelot.Testing.Authentication;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests.Authorization;

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
    public void Should_return_200_OK_authorizing_route()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        var claims = GivenRouteClaimsRequirement(route, "UserType", OcelotScopes.OcAdmin);
        var testName = TestName();
        this
            .Given(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIUpdateSubClaim())
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }

    [Fact]
    [Trait("Commit", "b8951c4")] // https://github.com/ThreeMammals/Ocelot/commit/b8951c4
    [Trait("Release", "1.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0-beta.1 -> https://github.com/ThreeMammals/Ocelot/releases/tag/1.1.0
    public void Should_return_403_Forbidden_authorizing_route()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        var claims = GivenRouteClaimsRequirement(route, "UserType", OcelotScopes.OcAdmin);
        route.AddClaimsToRequest.Remove("UserType"); // given I don't transform UserType claim
        var testName = TestName();
        this
            .Given(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIUpdateSubClaim())
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "100")] // https://github.com/ThreeMammals/Ocelot/issues/100
    [Trait("PR", "104")] // https://github.com/ThreeMammals/Ocelot/pull/104
    [Trait("Release", "1.4.5")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.5
    public void Should_return_200_OK_using_identity_server_with_allowed_scope()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        string[] allowedScopes = ["api", "api.readOnly", "openid", "offline_access"];
        var route = GivenAuthRoute(port, scopes: allowedScopes);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsExternalJwtSigningService(allowedScopes, CancelMe))
            .And(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveAToken("api.readOnly", null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBeAsync("Hello from Laura"))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "100")] // https://github.com/ThreeMammals/Ocelot/issues/100
    [Trait("PR", "104")] // https://github.com/ThreeMammals/Ocelot/pull/104
    [Trait("Release", "1.4.5")] // https://github.com/ThreeMammals/Ocelot/releases/tag/1.4.5
    public void Should_return_403_Forbidden_using_identity_server_with_scope_not_allowed()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        string[] allowedScopes = ["api", "openid", "offline_access"];
        var route = GivenAuthRoute(port, scopes: allowedScopes);
        var configuration = GivenConfiguration(route);
        var allScopes = allowedScopes.Append("api.readOnly").ToArray();
        this
            .Given(x => GivenThereIsExternalJwtSigningService(allScopes, CancelMe))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithScope("api.readOnly", testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
        .BDDfy();
    }

    [Fact(DisplayName = "TODO " + nameof(Should_fix_issue_240))]
    [Trait("Bug", "240")] // https://github.com/ThreeMammals/Ocelot/issues/240
    [Trait("PR", "243")] // https://github.com/ThreeMammals/Ocelot/pull/243
    [Trait("Release", "3.1.6")] // https://github.com/ThreeMammals/Ocelot/releases/tag/3.1.6
    public void Should_fix_issue_240()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        route.RouteClaimsRequirement = new() // TODO this is dictionary which doesn't support multiple keys of the same value
        {
            { ClaimTypes.Role, "User"}, // TODO Such a claim types in a form of URL (aka http://*) are not supported by JsonConfigurationProvider
            { nameof(ClaimTypes.Role), "User"}, // this key is Ok because it is not an URL containing proto delimiter aka '://'
        };
        var claims = new List<KeyValuePair<string, string>>()
        {
            new(nameof(ClaimTypes.Role), "AdminUser"),
            new(nameof(ClaimTypes.Role), "User"),
        };
        this
            .Given(x => GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }
    [Fact]
    [Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
    public void Should_return_200_OK_authorizing_route_with_url_shaped_claim_type()
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
        this
            .Given(x => GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }
    [Fact]
    [Trait("Bug", "679")] // https://github.com/ThreeMammals/Ocelot/issues/679
    public void Should_return_403_Forbidden_when_url_shaped_claim_type_missing()
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
        this
            .Given(x => GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithClaims(claims, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
        .BDDfy();
    }

    [Fact]
    [Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
    [Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
    [Trait("Release", "24.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
    public void Should_return_200_OK_with_global_allowed_scopes()
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AuthenticationProviderKeys = []; // no route auth!
        var configuration = GivenConfiguration(route);
        string[] globalScopes = ["api", "apiGlobal"];
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration(allowedScopes: globalScopes);
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenThereIsExternalJwtSigningService(globalScopes, CancelMe))
            .And(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveAToken("apiGlobal", null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyAsync(body))
        .BDDfy();
    }

    #region PR 1478
    [Fact]
    [Trait("Bug", "913")] // https://github.com/ThreeMammals/Ocelot/issues/913
    [Trait("PR", "1478")] // https://github.com/ThreeMammals/Ocelot/pull/1478
    [Trait("Release", "24.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/24.1.0
    public async Task Should_return_200_OK_with_space_separated_scope_match()
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scopes: ["api", "api.read", "api.write"]);
        var configuration = GivenConfiguration(route);
        string[] extraScopes = ["api.read", "openid", "offline_access"];
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenThereIsExternalJwtSigningService(extraScopes, CancelMe))
            .And(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithScope("api.read openid offline_access", testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyAsync(body))
        .BDDfy();
    }

    [Fact]
    [Trait("Bug", "913")]
    [Trait("PR", "1478")]
    [Trait("Release", "24.1.0")]
    public async Task Should_return_403_Forbidden_with_space_separated_scope_no_match()
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scopes: ["admin", "superuser"]);
        var configuration = GivenConfiguration(route);
        string[] extraScopes = ["api.read", "api.write", "openid"];
        this
            .Given(x => GivenThereIsExternalJwtSigningService(extraScopes, CancelMe))
            .And(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenIHaveATokenWithScope("api.read api.write openid", testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
        .BDDfy();
    }
    #endregion PR 1478
}
