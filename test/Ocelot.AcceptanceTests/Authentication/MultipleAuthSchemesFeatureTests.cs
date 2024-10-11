using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Ocelot.DependencyInjection;
using System.Net.Http.Headers;

namespace Ocelot.AcceptanceTests.Authentication;

[Trait("PR", "1870")]
[Trait("Issues", "740 1580")]
public sealed class MultipleAuthSchemesFeatureTests : AuthenticationSteps, IDisposable
{
    private IWebHost[] _identityServers;
    private string[] _identityServerUrls;
    private BearerToken[] _tokens;

    public MultipleAuthSchemesFeatureTests() : base()
    {
        _identityServers = Array.Empty<IWebHost>();
        _identityServerUrls = Array.Empty<string>();
        _tokens = Array.Empty<BearerToken>();
    }

    public override void Dispose()
    {
        foreach (var server in _identityServers)
        {
            server.Dispose();
        }

        base.Dispose();
    }

    private MultipleAuthSchemesFeatureTests Setup(int totalSchemes)
    {
        _identityServers = new IWebHost[totalSchemes];
        _identityServerUrls = new string[totalSchemes];
        _tokens = new BearerToken[totalSchemes];
        return this;
    }

    [Theory]
    [InlineData("Test1", "Test2")] // with multiple schemes
    [InlineData(IdentityServerAuthenticationDefaults.AuthenticationScheme, "Test")] // with default scheme
    [InlineData("Test", IdentityServerAuthenticationDefaults.AuthenticationScheme)] // with default scheme
    public void Should_authenticate_using_identity_server_with_multiple_schemes(string scheme1, string scheme2)
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultAuthRoute(port, authProviderKey: string.Empty);
        var authSchemes = new string[] { scheme1, scheme2 };
        route.AuthenticationOptions.AuthenticationProviderKeys = authSchemes;
        var configuration = GivenConfiguration(route);
        var responseBody = nameof(Should_authenticate_using_identity_server_with_multiple_schemes);

        this.Given(x => GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, responseBody))
            .And(x => Setup(authSchemes.Length)
                .GivenIdentityServerWithScopes(0, "invalid", "unknown")
                .GivenIdentityServerWithScopes(1, "api1", "api2"))
            .And(x => GivenIHaveTokenWithScope(0, "invalid")) // authentication should fail because of invalid scope
            .And(x => GivenIHaveTokenWithScope(1, "api2")) // authentication should succeed

            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningWithIdentityServerAuthSchemes("api2", authSchemes))
            .And(x => GivenIHaveAddedAllAuthHeaders(authSchemes))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(responseBody))
            .BDDfy();
    }

    private MultipleAuthSchemesFeatureTests GivenIdentityServerWithScopes(int index, params string[] scopes)
    {
        var tokenType = AccessTokenType.Jwt;
        string url = _identityServerUrls[index] = $"http://localhost:{PortFinder.GetRandomPort()}";
        var clients = new Client[] { DefaultClient(tokenType, scopes) };
        var builder = CreateIdentityServer(url, tokenType, scopes, clients);

        var server = _identityServers[index] = builder.Build();
        server.Start();
        VerifyIdentityServerStarted(url).GetAwaiter().GetResult();
        return this;
    }

    private async Task GivenIHaveTokenWithScope(int index, string scope)
    {
        string url = _identityServerUrls[index];
        _tokens[index] = await GivenAuthToken(url, scope);
    }

    private async Task GivenIHaveExpiredTokenWithScope(string url, string scope, int index)
    {
        _tokens[index] = await GivenAuthToken(url, scope, "expired");
    }

    private void GivenIHaveAddedAllAuthHeaders(string[] schemes)
    {
        // Assume default scheme token is attached as "Authorization" header, for example "Bearer"
        // But default authentication setup should be ignored in multiple schemes scenario
        _ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "failed");

        for (int i = 0; i < schemes.Length && i < _tokens.Length; i++)
        {
            var token = _tokens[i];
            var header = AuthHeaderName(schemes[i]);
            var hvalue = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            GivenIAddAHeader(header, hvalue.ToString());
        }
    }

    private static string AuthHeaderName(string scheme) => $"Oc-{HeaderNames.Authorization}-{scheme}";

    private void GivenOcelotIsRunningWithIdentityServerAuthSchemes(string validScope, params string[] schemes)
    {
        const string DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
        GivenOcelotIsRunningWithServices(services =>
        {
            services.AddOcelot();
            var auth = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "MultipleSchemes";
                    options.DefaultChallengeScheme = "MultipleSchemes";
                });
            for (int i = 0; i < schemes.Length; i++)
            {
                var scheme = schemes[i];
                var identityServerUrl = _identityServerUrls[i];
                auth.AddIdentityServerAuthentication(scheme, o =>
                {
                    o.Authority = identityServerUrl;
                    o.ApiName = validScope;
                    o.ApiSecret = "secret";
                    o.RequireHttpsMetadata = false;
                    o.SupportedTokens = SupportedTokens.Both;

                    // TODO TokenRetriever ?
                    o.ForwardDefaultSelector = (context) =>
                    {
                        var headers = context.Request.Headers;
                        var name = AuthHeaderName(scheme);
                        if (headers.ContainsKey(name))
                        {
                            // Redirect to default authentication handler which is (JwtAuthHandler) aka (Bearer)
                            headers[HeaderNames.Authorization] = headers[name];
                            return scheme;
                        }

                        // Something wrong with the setup: no headers, no tokens.
                        // Redirect to default scheme to read token from default header
                        return DefaultScheme;
                    };
                });
            }
        });
    }
}
