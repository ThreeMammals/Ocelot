using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;

namespace Ocelot.AcceptanceTests.Authentication;

public sealed class AuthenticationTests : AuthenticationSteps
{
    public AuthenticationTests()
    { }

    [Fact]
    public void Should_return_401_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this.Given(x => GivenThereIsExternalJwtSigningService())
           .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty))
           .And(x => GivenThereIsAConfiguration(configuration))
           .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
           .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
           .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
           .BDDfy();
    }

    [Fact]
    public async Task Should_return_response_200_using_identity_server()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenThereIsExternalJwtSigningService();
        await GivenIHaveAToken();
        GivenIHaveAddedATokenToMyRequest();

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
    }

    [Fact]
    public async Task Should_return_response_401_using_identity_server_with_token_requested_for_other_api()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);

        static void WithOtherApiAudience(JwtBearerOptions o)
        {
            o.Audience = "other.api.com";
            o.TokenValidationParameters.ValidAudience = "other.api.com";
        }
        void WithOtherApiBearerAuthentication(IServiceCollection services)
        {
            services.AddOcelot();
            Action<JwtBearerOptions> configureOptions = WithThreemammalsOptions;
            services.AddAuthentication().AddJwtBearer(configureOptions + WithOtherApiAudience);
        }
        GivenOcelotIsRunning(WithOtherApiBearerAuthentication);

        await GivenThereIsExternalJwtSigningService();
        var token = await GivenIHaveAToken(scope: "api2");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_return_201_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenThereIsExternalJwtSigningService();
        await GivenIHaveAToken();
        GivenIHaveAddedATokenToMyRequest();
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
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AuthenticationProviderKeys = []; // no route auth!
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration();
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK);
        await GivenThereIsExternalJwtSigningService();
        if (hasToken)
        {
            await GivenIHaveAToken();
            GivenIHaveAddedATokenToMyRequest();
        }

        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(status);
        ThenTheResponseBodyShouldBe(hasToken ? Body() : string.Empty);
    }

    [Fact]
    [Trait("PR", "2114")] // https://github.com/ThreeMammals/Ocelot/pull/2114
    [Trait("Feat", "842")] // https://github.com/ThreeMammals/Ocelot/issues/842
    public async Task Should_allow_anonymous_route_and_return_200_when_global_auth_options_and_no_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, allowAnonymous: true);
        route.AuthenticationOptions.AuthenticationProviderKeys = [];
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration();
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK);
        await GivenThereIsExternalJwtSigningService();

        // await GivenIHaveAToken();
        // GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");

        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBody();
    }

    [Fact]
    [Trait("Feat", "585")] // https://github.com/ThreeMammals/Ocelot/issues/585
    [Trait("Feat", "2316")] // https://github.com/ThreeMammals/Ocelot/issues/2316
    [Trait("PR", "2336")] // https://github.com/ThreeMammals/Ocelot/pull/2336
    public async Task ShouldApplyGlobalAuthenticationOptions_ForStaticRoutes()
    {
        var ports = PortFinder.GetPorts(3);
        var route1 = GivenAuthRoute(ports[0], "/route1",
            options: null); // no opts -> use global opts
        var route2 = GivenAuthRoute(ports[1], "/route2",
            GivenOptions(false, ["api"], "test", [JwtBearerDefaults.AuthenticationScheme]));
        var route3 = GivenAuthRoute(ports[2], "/noAuthorization",
            GivenOptions(false, ["invalid-scope"]));
        var configuration = GivenConfiguration(route1, route2, route3); // static routes come to Routes collection
        var globalOptions = configuration.GlobalConfiguration.AuthenticationOptions
            = new(GivenOptions(false, ["apiGlobal"], JwtBearerDefaults.AuthenticationScheme, []));

        GivenThereIsAServiceRunningOnPath(ports[0], "/route1");
        GivenThereIsAServiceRunningOnPath(ports[1], "/route2");
        GivenThereIsAServiceRunningOnPath(ports[2], "/noAuthorization");
        GivenThereIsAConfiguration(configuration);
        Action<IServiceCollection> withAuth = WithJwtBearerAuthentication;
        void WithOAuthNotConfigured(IServiceCollection services) => services
            .AddAuthentication()
            .AddOAuth(route2.AuthenticationOptions.AuthenticationProviderKey,
                opts => opts.ClientSecret = "bla-bla... actually, there are no options"); // -> 'test' scheme and it is registered now, but the auth will fail
        GivenOcelotIsRunning(withAuth + WithOAuthNotConfigured);
        await GivenThereIsExternalJwtSigningService("api", "apiGlobal", "Mr.Who");

        await GivenIHaveAToken(scope: globalOptions.AllowedScopes[0]);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route1");
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBody();

        await GivenIHaveAToken(scope: route2.AuthenticationOptions.AllowedScopes[0]);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route2");
        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBody();

        await GivenIHaveAToken(scope: "Mr.Who"); // should be different scope of route #3 which is "invalid-scope"
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/noAuthorization");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
        await ThenTheResponseBodyShouldBeEmpty();
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
            GivenOptions(false, ["invalid-scope"], JwtBearerDefaults.AuthenticationScheme));

        var configuration = GivenConfiguration(route1, route2, route3);
        var globalOptions = configuration.GlobalConfiguration.AuthenticationOptions
            = new(GivenOptions(false, ["apiGlobal"], JwtBearerDefaults.AuthenticationScheme, []))
            {
                RouteKeys = ["R2"],
            };

        GivenThereIsAServiceRunningOnPath(ports[0], "/route1");
        GivenThereIsAServiceRunningOnPath(ports[1], "/route2");
        GivenThereIsAServiceRunningOnPath(ports[2], "/noAuthorization");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithJwtBearerAuthentication);
        await GivenThereIsExternalJwtSigningService("api", "apiGlobal", "Mr.Who");

        await GivenIHaveAToken(scope: "Mr.Who");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route1");
        ThenTheStatusCodeShouldBeOK(); // auth is switched off and the scope doesn't matter
        ThenTheResponseBody();

        await GivenIHaveAToken(scope: globalOptions.AllowedScopes[0]);
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/route2");
        ThenTheStatusCodeShouldBeOK(); // global scope has been accepted
        ThenTheResponseBody();

        await GivenIHaveAToken(scope: "Mr.Who"); // should be different scope of route #3 which is "invalid-scope"
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/noAuthorization");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden);
        await ThenTheResponseBodyShouldBeEmpty();
    }
}
