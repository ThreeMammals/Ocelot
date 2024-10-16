using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.AcceptanceTests.Authentication;
using Ocelot.Configuration.File;
using System.Security.Claims;

namespace Ocelot.AcceptanceTests
{
    public class AuthorizationTests : AuthenticationSteps, IDisposable
    {
        private IWebHost _identityServerBuilder;
        private readonly Action<IdentityServerAuthenticationOptions> _options;
        private readonly string _identityServerRootUrl;
        private readonly ServiceHandler _serviceHandler;

        public AuthorizationTests()
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
        public void should_return_response_200_authorizing_route()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        AuthenticationOptions = new FileAuthenticationOptions
                        {
                            AuthenticationProviderKey = "Test",
                        },
                        AddHeadersToRequest =
                        {
                            {"CustomerId", "Claims[CustomerId] > value"},
                            {"LocationId", "Claims[LocationId] > value"},
                            {"UserType", "Claims[sub] > value[0] > |"},
                            {"UserId", "Claims[sub] > value[1] > |"},
                        },
                        AddClaimsToRequest =
                        {
                            {"CustomerId", "Claims[CustomerId] > value"},
                            {"UserType", "Claims[sub] > value[0] > |"},
                            {"UserId", "Claims[sub] > value[1] > |"},
                        },
                        RouteClaimsRequirement =
                        {
                            {"UserType", "registered"},
                        },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
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
        public void should_return_response_403_authorizing_route()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        AuthenticationOptions = new FileAuthenticationOptions
                        {
                            AuthenticationProviderKey = "Test",
                        },
                        AddHeadersToRequest =
                        {
                            {"CustomerId", "Claims[CustomerId] > value"},
                            {"LocationId", "Claims[LocationId] > value"},
                            {"UserType", "Claims[sub] > value[0] > |"},
                            {"UserId", "Claims[sub] > value[1] > |"},
                        },
                        AddClaimsToRequest =
                        {
                            {"CustomerId", "Claims[CustomerId] > value"},
                            {"UserId", "Claims[sub] > value[1] > |"},
                        },
                        RouteClaimsRequirement =
                        {
                            {"UserType", "registered"},
                        },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
                .And(x => GivenIHaveAToken(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_using_identity_server_with_allowed_scope()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        AuthenticationOptions = new FileAuthenticationOptions
                        {
                            AuthenticationProviderKey = "Test",
                            AllowedScopes = new List<string>{ "api", "api.readOnly", "openid", "offline_access" },
                        },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
                .And(x => GivenIHaveATokenForApiReadOnlyScope(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_403_using_identity_server_with_scope_not_allowed()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        AuthenticationOptions = new FileAuthenticationOptions
                        {
                            AuthenticationProviderKey = "Test",
                            AllowedScopes = new List<string>{ "api", "openid", "offline_access" },
                        },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
                .And(x => GivenIHaveATokenForApiReadOnlyScope(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
                .BDDfy();
        }

        [Fact]
        public void should_fix_issue_240()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        AuthenticationOptions = new FileAuthenticationOptions
                        {
                            AuthenticationProviderKey = "Test",
                        },
                        RouteClaimsRequirement =
                        {
                            {"Role", "User"},
                        },
                    },
                },
            };

            var users = new List<TestUser>
            {
                new()
                {
                    Username = "test",
                    Password = "test",
                    SubjectId = "registered|1231231",
                    Claims = new List<Claim>
                    {
                        new("Role", "AdminUser"),
                        new("Role", "User"),
                    },
                },
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt, users))
                .And(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", 200, "Hello from Laura"))
                .And(x => GivenIHaveAToken(_identityServerRootUrl))
                .And(x => GivenThereIsAConfiguration(configuration))
                .And(x => GivenOcelotIsRunning(_options, "Test"))
                .And(x => GivenIHaveAddedATokenToMyRequest())
                .When(x => WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
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

        private async Task GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType)
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
                            new("openid", "test"),
                            new("offline_access", "test"),
                            new("api.readOnly", "test"),
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
                                    new()
                                    {
                                        Value = "secret".Sha256(),
                                    },
                                },
                                UserClaims = new List<string>
                                {
                                    "CustomerId", "LocationId", "UserType", "UserId",
                                },
                            },
                        })
                        .AddInMemoryClients(new List<Client>
                        {
                            new()
                            {
                                ClientId = "client",
                                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                                ClientSecrets = new List<Secret> {new("secret".Sha256())},
                                AllowedScopes = new List<string> { apiName, "api.readOnly", "openid", "offline_access" },
                                AccessTokenType = tokenType,
                                Enabled = true,
                                RequireClientSecret = false,
                            },
                        })
                        .AddTestUsers(new List<TestUser>
                        {
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
                        });
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                })
                .Build();

            await _identityServerBuilder.StartAsync();

            await Steps.VerifyIdentityServerStarted(url);
        }

        private async Task GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType, List<TestUser> users)
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
                                    new()
                                    {
                                        Value = "secret".Sha256(),
                                    },
                                },
                                UserClaims = new List<string>
                                {
                                    "CustomerId", "LocationId", "UserType", "UserId", "Role",
                                },
                            },
                        })
                        .AddInMemoryClients(new List<Client>
                        {
                            new()
                            {
                                ClientId = "client",
                                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                                ClientSecrets = new List<Secret> {new("secret".Sha256())},
                                AllowedScopes = new List<string> { apiName, "api.readOnly", "openid", "offline_access" },
                                AccessTokenType = tokenType,
                                Enabled = true,
                                RequireClientSecret = false,
                            },
                        })
                        .AddTestUsers(users);
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                })
                .Build();

            await _identityServerBuilder.StartAsync();

            await Steps.VerifyIdentityServerStarted(url);
        }

        private async Task GivenIHaveATokenForApiReadOnlyScope(string url)
            => await GivenAuthToken(url, "api.readOnly");

        public override void Dispose()
        {
            _serviceHandler?.Dispose();
            _identityServerBuilder?.Dispose();
            base.Dispose();
        }
    }
}
