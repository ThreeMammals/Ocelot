using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Ocelot.DependencyInjection;
using Ocelot.Testing.Authentication;

namespace Ocelot.AcceptanceTests.Authentication;

public sealed class AuthenticationTests : AuthenticationSteps
{
    //public IFluentStepBuilder<AuthenticationTests> GivenScenario(Expression<Func<AuthenticationTests, Task>> step)
    //{
    //    return FluentStepScannerExtensions.Given(this, step);
    //    //return new FluentStepBuilder<AuthenticationTests>(this).Given(step);
    //}
    //public static IFluentStepBuilder<TScenario> GivenScenario2<TScenario>(TScenario testObject, Expression<Func<TScenario, Task>> step)
    //    where TScenario : class
    //{
    //    return new FluentStepBuilder<TScenario>(testObject).Given(step);
    //}

    [BddfyFact]
    public void Should_return_401_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        //GivenScenario(x => GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe))
        //    .BDDfy();
        //GivenScenario2(this, x => GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe))
        //    .BDDfy();
        this
            .Given(x => GivenThereIsExternalJwtSigningService(Array.Empty<string>(), CancelMe))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
    }

    [BddfyFact]
    public void Should_return_response_200_using_identity_server()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            .And(x => GivenIHaveAToken(testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
        .BDDfy();
    }

    [BddfyFact]
    public async Task Should_return_response_401_using_identity_server_with_token_requested_for_other_api()
    {
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithOtherApiBearerAuthentication))
            .And(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            .And(x => GivenIHaveAToken("api2", null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
        .BDDfy();
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

    [BddfyFact]
    public async Task Should_return_201_using_identity_server_access_token()
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            .And(x => GivenIHaveAToken(testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
        .BDDfy();
    }

    [BddfyTheory]
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
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, body))
            .And(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            .And(x => GivenIHaveAddedATokenToMyRequestIfRequired(hasToken))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(status))
            .And(x => ThenTheResponseBodyShouldBe(hasToken ? body : string.Empty))
        .BDDfy();
    }
    private Task GivenIHaveAddedATokenToMyRequestIfRequired(bool hasToken) => hasToken
        ? GivenIHaveAToken().ContinueWith(t => GivenIHaveAddedATokenToMyRequest())
        : Task.CompletedTask;

    [BddfyFact]
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
        this
            .Given(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, body))
            .And(x => GivenThereIsExternalJwtSigningService(NoScopes, CancelMe))
            // await GivenIHaveAToken();
            // GivenIHaveAddedATokenToMyRequest();
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBody(body))
        .BDDfy();
    }

    [BddfyFact]
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
        this
            .Given(x => GivenThereIsAServiceRunningOnPath(ports[0], "/route1", body))
            .And(x => GivenThereIsAServiceRunningOnPath(ports[1], "/route2", body))
            .And(x => GivenThereIsAServiceRunningOnPath(ports[2], "/noAuthorization", body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(withAuth))
            .And(x => GivenThereIsExternalJwtSigningService(scopes, CancelMe))
            .And(x => GivenIHaveAToken(globalOptions.AllowedScopes[0], null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/route1"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBody(body))

            .Given(x => GivenIHaveAToken(route2.AuthenticationOptions.AllowedScopes[0], null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/route2"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBody(body))

            .Given(x => GivenIHaveAToken("Mr.Who", null, null, null, testName)) // should be different scope of route #3 which is "invalid-scope"
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/noAuthorization"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }

    [BddfyFact]
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
        this
            .Given(x => GivenThereIsAServiceRunningOnPath(ports[0], "/route1", body))
            .And(x => GivenThereIsAServiceRunningOnPath(ports[1], "/route2", body))
            .And(x => GivenThereIsAServiceRunningOnPath(ports[2], "/noAuthorization", body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithJwtBearerAuthentication))
            .And(x => GivenThereIsExternalJwtSigningService(scopes, CancelMe))
            .And(x => GivenIHaveAToken("Mr.Who", null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/route1"))
            .Then(x => ThenTheStatusCodeShouldBeOk()) // auth is switched off and the scope doesn't matter
            .And(x => ThenTheResponseBody(body))

            .Given(x => GivenIHaveAToken(globalOptions.AllowedScopes[0], null, null, null, testName))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/route2"))
            .Then(x => ThenTheStatusCodeShouldBeOk()) // global scope has been accepted
            .And(x => ThenTheResponseBody(body))

            .Given(x => GivenIHaveAToken("Mr.Who", null, null, null, testName)) // should be different scope of route #3 which is "invalid-scope"
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/noAuthorization"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
            .And(x => ThenTheResponseBodyShouldBeEmpty())
        .BDDfy();
    }
}
