using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.Authentication;

public sealed class AuthenticationTests : AuthenticationSteps
{
    public const string IdentityServer4Skip = "TODO: Redevelopment required due to IdentityServer4 being deprecated.";

    public AuthenticationTests()
    { }

    private static void Void() { }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_return_401_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
           .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty))
           .And(x => GivenThereIsAConfiguration(configuration))

           //.And(x => GivenOcelotIsRunning(_options, "Test"))
           .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
           .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
           .BDDfy();
    }

    [Fact(Skip = IdentityServer4Skip)]
    public async Task Should_return_response_200_using_identity_server()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        Void(); //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        _ = await GivenIHaveAToken();
        GivenThereIsAConfiguration(configuration);

        //GivenOcelotIsRunning(_options, "Test");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Laura");
    }

    [Fact(Skip = IdentityServer4Skip)]
    public async Task Should_return_response_401_using_identity_server_with_token_requested_for_other_api()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        Void(); //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura");
        var token = GivenIHaveAToken(scope: "api2");
        GivenThereIsAConfiguration(configuration);

        //GivenOcelotIsRunning(_options, "Test");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = IdentityServer4Skip)]
    public async Task Should_return_201_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        Void(); //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created);
        _ = await GivenIHaveAToken();
        GivenThereIsAConfiguration(configuration);

        //GivenOcelotIsRunning(_options, "Test");
        GivenIHaveAddedATokenToMyRequest();
        await WhenIPostUrlOnTheApiGateway("/", "postContent");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Created);
    }

    [Fact(Skip = IdentityServer4Skip)]
    public async Task Should_return_201_using_identity_server_reference_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, method: HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        Void(); //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Reference))
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created);
        _ = await GivenIHaveAToken();
        GivenThereIsAConfiguration(configuration);

        //GivenOcelotIsRunning(_options, "Test");
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
        await GivenThereIsAnIdentityServer();
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK);
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
    [Trait("PR", "2114")]
    [Trait("Feat", "842")]
    public async Task Should_allow_anonymous_route_and_return_200_when_global_auth_options_and_no_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, allowAnonymous: true);
        route.AuthenticationOptions.AuthenticationProviderKeys = [];
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration = GivenGlobalAuthConfiguration();
        await GivenThereIsAnIdentityServer();
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithAspNetIdentityAuthentication);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK);

        // await GivenIHaveAToken();
        // GivenIHaveAddedATokenToMyRequest();
        await WhenIGetUrlOnTheApiGateway("/");

        ThenTheStatusCodeShouldBeOK();
        ThenTheResponseBody();
    }
}
