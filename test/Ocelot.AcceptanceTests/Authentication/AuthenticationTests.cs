using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using System.Security.Claims;

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
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
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
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
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
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(DownstreamServiceUrl(port), 200, "Hello from Laura"))
                .And(x => GivenIHaveATokenForApi2(_identityServerRootUrl))
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
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
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
            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Reference))
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

        private void GivenThereIsAnIdentityServerOn(string url, string apiName, string api2Name, AccessTokenType tokenType)
        {
            _identityServerBuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddIdentityServer()
                        .AddDeveloperSigningCredential()
                        .AddInMemoryApiScopes(new List<ApiScope>
                        {
                            new(apiName, "test"),
                            new(api2Name, "test"),
                        })
                        .AddInMemoryApiResources(new List<ApiResource>
                        {
                            new()
                            {
                                Name = apiName,
                                Description = "My API",
                                Enabled = true,
                                DisplayName = "test",
                                Scopes = new List<string>
                                {
                                    "api",
                                    "api.readOnly",
                                    "openid",
                                    "offline_access",
                                },
                                ApiSecrets = new List<Secret>
                                {
                                    new("secret".Sha256()),
                                },
                                UserClaims = new List<string>
                                {
                                    "CustomerId", "LocationId",
                                },
                            },
                            new()
                            {
                                Name = api2Name,
                                Description = "My second API",
                                Enabled = true,
                                DisplayName = "second test",
                                Scopes = new List<string>
                                {
                                    "api2",
                                    "api2.readOnly",
                                },
                                ApiSecrets = new List<Secret>
                                {
                                    new("secret".Sha256()),
                                },
                                UserClaims = new List<string>
                                {
                                    "CustomerId", "LocationId",
                                },
                            },
                        })
                        .AddInMemoryClients(new List<Client>
                        {
                            new()
                            {
                                ClientId = "client",
                                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                                ClientSecrets = new List<Secret> { new("secret".Sha256()) },
                                AllowedScopes = new List<string> { apiName, api2Name, "api.readOnly", "openid", "offline_access" },
                                AccessTokenType = tokenType,
                                Enabled = true,
                                RequireClientSecret = false,
                            },
                        })
                        .AddTestUsers(
                        [
                            new()
                            {
                                Username = "test",
                                Password = "test",
                                SubjectId = "registered|1231231",
                                Claims = new List<Claim>
                                {
                                   new("CustomerId", "123"),
                                   new("LocationId", "321"),
                                },
                            },
                        ]);
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                })
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
