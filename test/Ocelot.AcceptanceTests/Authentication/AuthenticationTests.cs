using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests.Authentication
{
    public sealed class AuthenticationTests : AuthenticationSteps, IDisposable
    {
        private IWebHost _identityServerBuilder;
        private readonly string _identityServerRootUrl;
        private readonly Action<IdentityServerAuthenticationOptions> _options;

        public AuthenticationTests()
        {
            var identityServerPort = PortFinder.GetRandomPort();
            _identityServerRootUrl = $"http://localhost:{identityServerPort}";
            _options = o =>
            {
                o.Authority = _identityServerRootUrl;
                o.ApiName = "api";
                o.RequireHttpsMetadata = false;
                o.SupportedTokens = SupportedTokens.Both;
                o.ApiSecret = "secret";
            };
        }

        [Fact]
        public void Should_return_401_using_identity_server_access_token()
        {
            var port = PortFinder.GetRandomPort();
            var route = GivenDefaultAuthRoute(port, HttpMethods.Post);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), HttpStatusCode.Created, string.Empty))
               .And(x => GivenThereIsAConfiguration(configuration))
               .And(x => GivenOcelotIsRunning(_options, "Test"))
               .And(x => GivenThePostHasContent("postContent"))
               .When(x => WhenIPostUrlOnTheApiGateway("/"))
               .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
               .BDDfy();
        }

        [Fact]
        public void Should_return_response_200_using_identity_server()
        {
            var port = PortFinder.GetRandomPort();
            var route = GivenDefaultAuthRoute(port);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), HttpStatusCode.OK, "Hello from Laura"))
                .And(x => GivenIHaveAToken(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void Should_return_response_401_using_identity_server_with_token_requested_for_other_api()
        {
            var port = PortFinder.GetRandomPort();
            var route = GivenDefaultAuthRoute(port);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), HttpStatusCode.OK, "Hello from Laura"))
                .And(x => GivenAuthToken(_identityServerRootUrl, "api2"))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
                .BDDfy();
        }

        [Fact]
        public void Should_return_201_using_identity_server_access_token()
        {
            var port = PortFinder.GetRandomPort();
            var route = GivenDefaultAuthRoute(port, HttpMethods.Post);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), HttpStatusCode.Created, string.Empty))
                .And(x => GivenIHaveAToken(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .And(x => GivenThePostHasContent("postContent"))
                .When(x => WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        [Fact]
        public void Should_return_201_using_identity_server_reference_token()
        {
            var port = PortFinder.GetRandomPort();
            var route = GivenDefaultAuthRoute(port, HttpMethods.Post);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Reference))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), HttpStatusCode.Created, string.Empty))
                .And(x => GivenIHaveAToken(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .And(x => GivenThePostHasContent("postContent"))
                .When(x => WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        [IgnorePublicMethod]
        public async Task GivenThereIsAnIdentityServerOn(string url, AccessTokenType tokenType)
        {
            var scopes = new string[] { "api", "api2" };
            _identityServerBuilder = CreateIdentityServer(url, tokenType, scopes, null)
                .Build();
            await _identityServerBuilder.StartAsync();
            await VerifyIdentityServerStarted(url);
        }

        public override void Dispose()
        {
            _identityServerBuilder?.Dispose();
            base.Dispose();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class IgnoreXunitAnalyzersRule1013Attribute : Attribute { }

    [IgnoreXunitAnalyzersRule1013]
    [AttributeUsage(AttributeTargets.Method)]
    public class IgnorePublicMethodAttribute : Attribute { }
}
