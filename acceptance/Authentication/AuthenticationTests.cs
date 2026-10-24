using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;

namespace Ocelot.Acceptance.Authentication;

public sealed class AuthenticationTests : AuthSteps
{
    [Fact]
    public async Task Should_return_401_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        await GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await WhenIPostUrlOnTheApiGateway("/", "postContent");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_return_response_200_using_identity_server()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        await GivenIHaveAToken(testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
    }

    [Fact]
    public async Task Should_return_response_401_using_identity_server_with_token_requested_for_other_api()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithOtherApiBearerAuthentication);
        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        await GivenIHaveAToken("api2", null, null, null, testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized);
    }
    private static void WithOtherApiAudience(JwtBearerOptions o)
    {
        o.Audience = "other.api.com";
        o.TokenValidationParameters.ValidAudience = "other.api.com";
    }
    private void WithOtherApiBearerAuthentication(IServiceCollection services)
    {
        services.AddOcelot();
        Action<JwtBearerOptions> configureOptions = WithThreemammalsOptions;
        services.AddAuthentication().AddJwtBearer(configureOptions + WithOtherApiAudience);
    }

    [Fact]
    public async Task Should_return_201_using_identity_server_access_token()
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        await GivenIHaveAToken(testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIPostUrlOnTheApiGateway("/", "postContent");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
    [Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.Unauthorized)]
    public async Task Should_use_global_authentication(bool hasToken, HttpStatusCode status)
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AuthenticationProviderKeys = []; // no route auth!
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration();

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, body);
        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        await GivenIHaveAddedATokenToMyRequestIfRequired(hasToken);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(status);
        ThenTheResponseBodyShouldBe(hasToken ? body : string.Empty);
    }

    private async Task GivenIHaveAddedATokenToMyRequestIfRequired(bool hasToken) => hasToken
        ? await GivenIHaveAToken().ContinueWith(t => GivenIHaveAddedATokenToMyRequest())
        : Task.CompletedTask;

    [Fact]
    [Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
    [Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
    public async Task Should_allow_anonymous_route_and_return_200_when_global_auth_options_and_no_token()
    {
        var body = Body();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, allowAnonymous: true);
        route.AuthenticationOptions.AuthenticationProviderKeys = [];
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration();

        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, body);
        await GivenThereIsExternalJwtSigningService(NoScopes, CancelMe);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseBody(body);
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public async Task ShouldApplyGlobalAuthenticationOptions_ForStaticRoutes()
    {
        var body = Body();
        var testName = TestName();
        var ports = PortFinder.GetPorts(3);
        var route1 = GivenAuthRoute(ports[0], "/route1",
            options: null); // no opts -> use global opts
        var route2 = GivenAuthRoute(ports[1], "/route2",
            GivenOptions(false, ["api"], ["test", JwtBearerDefaults.AuthenticationScheme]));
        var route3 = GivenAuthRoute(ports[2], "/noAuthorization",
            GivenOptions(false, ["invalid-scope"]));
        var configuration = GivenConfiguration(route1, route2, route3); // static routes come to Routes collection
        var globalOptions = configuration.GlobalConfiguration.AuthenticationOptions
            = new(GivenOptions(false, ["apiGlobal"], [JwtBearerDefaults.AuthenticationScheme]));

        void WithOAuthNotConfigured(IServiceCollection services) => services
            .AddAuthentication()
            .AddOAuth(route2.AuthenticationOptions.AuthenticationProviderKeys[0],
                opts => opts.ClientSecret = "bla-bla... actually, there are no options"); // -> 'test' scheme and it is registered now, but the auth will fail
        Action<IServiceCollection> withAuth = WithJwtBearerAuthentication;
        withAuth += WithOAuthNotConfigured;

        var scopes = new string[] { "api", "apiGlobal", "Mr.Who" };

        GivenThereIsAServiceRunningOnPath(ports[0], "/route1", body);
        GivenThereIsAServiceRunningOnPath(ports[1], "/route2", body);
        GivenThereIsAServiceRunningOnPath(ports[2], "/noAuthorization", body);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(withAuth);
        await GivenThereIsExternalJwtSigningService(scopes, CancelMe);
        await GivenIHaveAToken(globalOptions.AllowedScopes[0], null, null, null, testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route1");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseBody(body);

        await GivenIHaveAToken(route2.AuthenticationOptions.AllowedScopes[0], null, null, null, testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route2");
        ThenTheStatusCodeShouldBeOk();
        ThenTheResponseBody(body);

        await GivenIHaveAToken("Mr.Who", null, null, null, testName); // should be different scope of route #3 which is "invalid-scope"
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/noAuthorization");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
        ThenTheResponseBodyShouldBeEmpty();
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public async Task ShouldApplyGlobalGroupAuthenticationOptions_ForStaticRoutes_WhenRouteOptsHasAKey()
    {
        // 1st route
        var ports = PortFinder.GetPorts(3);
        var route1 = GivenAuthRoute(ports[0], "/route1", options: null); // no opts -> no auth at all
        route1.Key = null; // 1st route is not in the global group

        // 2nd route
        var route2 = GivenAuthRoute(ports[1], "/route2", options: null); // 2nd route opts will be applied from global ones
        route2.Key = "R2"; // 2nd route is in the group

        // 3rd route
        var route3 = GivenAuthRoute(ports[2], "/noAuthorization",
            GivenOptions(false, ["invalid-scope"], [JwtBearerDefaults.AuthenticationScheme]));

        var configuration = GivenConfiguration(route1, route2, route3);
        var globalOptions = configuration.GlobalConfiguration.AuthenticationOptions
            = new(GivenOptions(false, ["apiGlobal"], [JwtBearerDefaults.AuthenticationScheme]))
            {
                RouteKeys = ["R2"],
            };

        var body = Body();
        var testName = TestName();
        var scopes = new string[] { "api", "apiGlobal", "Mr.Who" };

        GivenThereIsAServiceRunningOnPath(ports[0], "/route1", body);
        GivenThereIsAServiceRunningOnPath(ports[1], "/route2", body);
        GivenThereIsAServiceRunningOnPath(ports[2], "/noAuthorization", body);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenThereIsExternalJwtSigningService(scopes, CancelMe);

        await GivenIHaveAToken("Mr.Who", null, null, null, testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route1");
        ThenTheStatusCodeShouldBeOk(); // auth is switched off and the scope doesn't matter
        ThenTheResponseBody(body);

        await GivenIHaveAToken(globalOptions.AllowedScopes[0], null, null, null, testName);
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route2");
        ThenTheStatusCodeShouldBeOk(); // global scope has been accepted
        ThenTheResponseBody(body);

        await GivenIHaveAToken("Mr.Who", null, null, null, testName); // should be different scope of route #3 which is "invalid-scope"
        await GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/noAuthorization");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
        ThenTheResponseBodyShouldBeEmpty();
    }
}
