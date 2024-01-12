using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;

namespace Ocelot.AcceptanceTests.Authentication;

public class MultipleAuthSchemesFeatureTests : AuthenticationSteps, IDisposable
{
    private IWebHost _identityServer;
    private readonly string _identityServerUrl;

    public MultipleAuthSchemesFeatureTests() : base()
    {
        var port = PortFinder.GetRandomPort();
        _identityServerUrl = $"http://localhost:{port}";
    }

    public override void Dispose()
    {
        _identityServer.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_authenticate_with_response_200_using_one_identity_server_with_two_tokens()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        Action<IdentityServerAuthenticationOptions> options = o =>
        {
            o.Authority = _identityServerUrl;
            o.ApiName = "api";
            o.RequireHttpsMetadata = false;
            o.SupportedTokens = SupportedTokens.Both;
            o.ApiSecret = "secret";
        };
        var responseBody = "Hello from test " + nameof(Should_authenticate_with_response_200_using_one_identity_server_with_two_tokens);
        this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerUrl, AccessTokenType.Jwt))

            // TODO Check below
            .And(x => x.GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, responseBody))
            .And(x => GivenIHaveAToken(_identityServerUrl))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(options, "Test"))
            .And(x => GivenIHaveAddedATokenToMyRequest())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBody))
            .BDDfy();
    }

    protected void GivenThereIsAnIdentityServerOn(string url, AccessTokenType tokenType)
    {
        var builder = CreateIdentityServer(url, tokenType, "api", "api2");
        _identityServer = builder.Build();
        _identityServer.Start();
        VerifyIdentityServerStarted(url);
    }
}
