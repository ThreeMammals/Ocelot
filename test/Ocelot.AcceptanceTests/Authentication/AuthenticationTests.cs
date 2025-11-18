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
        ThenTheResponseBodyShouldBe(hasToken ? nameof(Should_use_global_authentication) : string.Empty);
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
}
