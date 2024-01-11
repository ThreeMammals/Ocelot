using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests.Authentication
{
    public sealed class AuthenticationTests : AuthenticationSteps, IDisposable
    {
        private IWebHost _identityServerBuilder;
        private readonly string _identityServerRootUrl;
        private readonly Action<IdentityServerAuthenticationOptions> _options;
        private readonly ServiceHandler _serviceHandler;

        public AuthenticationTests()
        {
            _serviceHandler = new ServiceHandler();
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
            var route = GivenDefaultRoute(port, HttpMethods.Post);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), 201, string.Empty))
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
            var route = GivenDefaultRoute(port);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), 200, "Hello from Laura"))
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
            var route = GivenDefaultRoute(port);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), 200, "Hello from Laura"))
                .And(x => GivenIHaveATokenWithScope(_identityServerRootUrl, "api2"))
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
            var route = GivenDefaultRoute(port, HttpMethods.Post);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), 201, string.Empty))
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
            var route = GivenDefaultRoute(port, HttpMethods.Post);
            var configuration = GivenConfiguration(route);
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, AccessTokenType.Reference))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), 201, string.Empty))
                .And(x => GivenIHaveAToken(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .And(x => GivenThePostHasContent("postContent"))
                .When(x => WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
        {
            _serviceHandler.GivenThereIsAServiceRunningOn(url, async context =>
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            });
        }

        private void GivenThereIsAnIdentityServerOn(string url, AccessTokenType tokenType)
        {
            _identityServerBuilder = CreateIdentityServer(url, tokenType, "api", "api2")
                .Build();
            _identityServerBuilder.Start();
            VerifyIdentityServerStarted(url);
        }

        private static FileRoute GivenDefaultRoute(int port, string upstreamHttpMethod = null) => new()
        {
            DownstreamPathTemplate = "/",
            DownstreamHostAndPorts =
                [
                    new("localhost", port),
                ],
            DownstreamScheme = Uri.UriSchemeHttp,
            UpstreamPathTemplate = "/",
            UpstreamHttpMethod =[upstreamHttpMethod ?? HttpMethods.Get],
            AuthenticationOptions = new FileAuthenticationOptions
            {
                AuthenticationProviderKey = "Test",
            },
        };

        private static FileConfiguration GivenConfiguration(params FileRoute[] routes)
        {
            var configuration = new FileConfiguration();
            configuration.Routes.AddRange(routes);
            return configuration;
        }

        private static string DownstreamServiceUrl(int port) => string.Concat("http://localhost:", port);

        public override void Dispose()
        {
            _serviceHandler.Dispose();
            _identityServerBuilder?.Dispose();
            base.Dispose();
        }
    }
}
