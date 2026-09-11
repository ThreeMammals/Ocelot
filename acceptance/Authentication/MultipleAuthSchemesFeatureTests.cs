using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Ocelot.DependencyInjection;
using Ocelot.Testing.Authentication;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace Ocelot.Acceptance.Authentication;

[Trait("PR", "1870")] // https://github.com/ThreeMammals/Ocelot/pull/1870
[Trait("Feat", "740")] // https://github.com/ThreeMammals/Ocelot/issues/740
[Trait("Feat", "1580")] // https://github.com/ThreeMammals/Ocelot/issues/1580
public sealed class MultipleAuthSchemesFeatureTests : AuthSteps
{
    private string[] _serverUrls;
    private BearerToken[] _tokens;

    public MultipleAuthSchemesFeatureTests() : base()
    {
        _serverUrls = Array.Empty<string>();
        _tokens = Array.Empty<BearerToken>();
    }

    private MultipleAuthSchemesFeatureTests Setup(int totalSchemes)
    {
        _serverUrls = new string[totalSchemes];
        _tokens = new BearerToken[totalSchemes];
        return this;
    }

    [Theory]
    [InlineData("Test1", "Test2")] // with multiple schemes
    [InlineData(JwtBearerDefaults.AuthenticationScheme, "Test")] // with default scheme
    [InlineData("Test", JwtBearerDefaults.AuthenticationScheme)] // with default scheme
    public async Task Should_authenticate_using_multiple_schemes(string scheme1, string scheme2)
    {
        var body = Body();
        var testName = TestName();
        var port = PortFinder.GetRandomPort();
        var route = GivenAuthRoute(port, scheme: "bla-bla"); //, validScope: "api2"); // TODO Need further dev
        string[] authSchemes = new[] { scheme1, scheme2 };
        route.AuthenticationOptions.AuthenticationProviderKeys = authSchemes;
        var configuration = GivenConfiguration(route);
        this
            .Given(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => Setup(authSchemes.Length))
            .And(x => GivenThereIsExternalJwtSigningService(0, "invalid", "unknown"))
            .And(x => GivenThereIsExternalJwtSigningService(1, "api1", "api2"))
            .And(x => GivenOcelotIsRunningWithIdentityServerAuthSchemes("api2", authSchemes))
            .And(x => GivenIHaveTokenWithScope(0, "invalid", testName)) // authentication should fail because of invalid scope
            .And(x => GivenIHaveTokenWithScope(1, "api2", testName)) // authentication should succeed
            .And(x => GivenIHaveAddedAllAuthHeaders(authSchemes))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBeOk())
            .And(x => ThenTheResponseBodyShouldBe(body))
        .BDDfy();
    }

    private async Task GivenThereIsExternalJwtSigningService(int index, params string[] extraScopes)
    {
        _serverUrls[index] = await GivenThereIsExternalJwtSigningService(extraScopes, CancelMe);
    }

    private async Task GivenIHaveTokenWithScope(int index, string scope, [CallerMemberName] string testName = "")
    {
        string url = _serverUrls[index];
        _tokens[index] = await GivenIHaveAToken(scope, null, url, testName);
    }

    private void GivenIHaveAddedAllAuthHeaders(string[] schemes)
    {
        // Assume default scheme token is attached as "Authorization" header, for example "Bearer"
        // But default authentication setup should be ignored in multiple schemes scenario
        ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "failed");

        for (int i = 0; i < schemes.Length && i < _tokens.Length; i++)
        {
            var token = _tokens[i];
            var header = AuthHeaderName(schemes[i]);
            var hvalue = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            GivenIAddAHeader(header, hvalue.ToString());
        }
    }

    private static string AuthHeaderName(string scheme) => $"Oc-{HeaderNames.Authorization}-{scheme}";

    private void WithBearerOptions(JwtBearerOptions o, string scheme, string issuerUrl)
    {
        AuthenticationTokenRequest request = AuthTokens[issuerUrl];
        string authority = new Uri(JwtSigningServerUrl).Authority;
        o.Audience = request.Audience;
        o.Authority = authority;
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = request.Audience, // ocelotClient.BaseAddress.Authority,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = request.IssuerSigningKey(),
        };
        o.ForwardDefaultSelector = (context) => // TODO TokenRetriever ?
        {
            var headers = context.Request.Headers;
            var name = AuthHeaderName(scheme);
            if (headers.TryGetValue(name, out StringValues value))
            {
                // Redirect to default authentication handler which is (JwtAuthHandler) aka (Bearer)
                headers[HeaderNames.Authorization] = value;
                return scheme;
            }

            // Something wrong with the setup: no headers, no tokens.
            // Redirect to default scheme to read token from default header
            return JwtBearerDefaults.AuthenticationScheme;
        };
    }

    private void GivenOcelotIsRunningWithIdentityServerAuthSchemes(string validScope, params string[] schemes)
    {
        GivenOcelotIsRunning(services =>
        {
            var ocelot = services.AddOcelot();
            var auth = services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            for (int i = 0; i < schemes.Length; i++)
            {
                var scheme = schemes[i];
                var issuerUrl = _serverUrls[i];
                auth.AddJwtBearer(scheme,
                    o => WithBearerOptions(o, scheme, issuerUrl));
            }
        });
    }
}
