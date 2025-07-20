//using IdentityServer4.AccessTokenValidation;
//using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.Authentication;

public sealed class AuthenticationTests : AuthenticationSteps
{
    //private readonly IWebHost _identityServerBuilder;
    private readonly string _identityServerRootUrl;

    //private readonly Action<IdentityServerAuthenticationOptions> _options;
    public const string IdentityServer4Skip = "TODO: Redevelopment required due to IdentityServer4 being deprecated.";

    public AuthenticationTests()
    {
        var identityServerPort = PortFinder.GetRandomPort();
        _identityServerRootUrl = $"http://localhost:{identityServerPort}";

        //_options = o =>
        //{
        //    o.Authority = _identityServerRootUrl;
        //    o.ApiName = "api";
        //    o.RequireHttpsMetadata = false;
        //    o.SupportedTokens = SupportedTokens.Both;
        //    o.ApiSecret = "secret";
        //};
    }

    private static void Void() { }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_return_401_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, HttpMethods.Post);
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
    public void Should_return_response_200_using_identity_server()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenIHaveAToken())
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_return_response_401_using_identity_server_with_token_requested_for_other_api()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        var configuration = GivenConfiguration(route);
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, "Hello from Laura"))
            .And(x => GivenAuthToken(_identityServerRootUrl, "api2"))
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
            .BDDfy();
    }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_return_201_using_identity_server_access_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty))
            .And(x => GivenIHaveAToken())
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
            .BDDfy();
    }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_return_201_using_identity_server_reference_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        this.Given(x => Void()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Reference))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.Created, string.Empty))
            .And(x => GivenIHaveAToken())
            .And(x => GivenThereIsAConfiguration(configuration))

            //.And(x => GivenOcelotIsRunning(_options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIPostUrlOnTheApiGateway("/", "postContent"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
            .BDDfy();
    }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_use_global_authentication_and_return_401_when_no_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port);
        route.AuthenticationOptions.AuthenticationProviderKeys = null;
        var globalConfig = GivenGlobalConfig();
        var configuration = GivenConfiguration(globalConfig, route);
        this.Given(x => x.random.Next()) //x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Reference))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            //.And(x => GivenOcelotIsRunning(_options, "key"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
            .BDDfy();
    }

    [Fact(Skip = IdentityServer4Skip)]
    public void Should_allow_anonymous_route_and_return_200_when_global_auth_options_and_no_token()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, allowAnonymous: true);
        route.AuthenticationOptions.AuthenticationProviderKeys = null;
        var globalConfig = GivenGlobalConfig();
        var configuration = GivenConfiguration(globalConfig, route);
        this.Given(x => x.random.Next())//x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Reference))
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, string.Empty))
            .And(x => GivenThereIsAConfiguration(configuration))
            //.And(x => GivenOcelotIsRunning(_options, "key"))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    //public async Task GivenThereIsAnIdentityServerOn(string url, AccessTokenType tokenType)
    //{
    //    var scopes = new string[] { "api", "api2" };
    //    _identityServerBuilder = CreateIdentityServer(url, tokenType, scopes, null)
    //        .Build();
    //    await _identityServerBuilder.StartAsync();
    //    await VerifyIdentityServerStarted(url);
    //}
    public override void Dispose()
    {
        //_identityServerBuilder?.Dispose();
        base.Dispose();
    }

    private async Task GivenIHaveAToken() => token = await GivenIHaveAToken(_identityServerRootUrl);
}
