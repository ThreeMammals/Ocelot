using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using TestStack.BDDfy;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ocelot.AcceptanceTests
{
    using IdentityServer4;
    using IdentityServer4.Test;

    public class ClaimsToHeadersForwardingTests : IDisposable
    {
        private IWebHost _servicebuilder;
        private IWebHost _identityServerBuilder;
        private readonly Steps _steps;

        public ClaimsToHeadersForwardingTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_and_foward_claim_as_header()
        {
            var user = new TestUser()
            {
                Username = "test",
                Password = "test",
                SubjectId = "registered|1231231",
                Claims = new List<Claim>
                {
                    new Claim("CustomerId", "123"),
                    new Claim("LocationId", "1")
                }
            };

            var configuration = new FileConfiguration
            {
                ReRoutes = new List<FileReRoute>
                    {
                        new FileReRoute
                        {
                            DownstreamPathTemplate = "/",
                            DownstreamPort = 52876,
                            DownstreamScheme = "http",
                            DownstreamHost = "localhost",
                            UpstreamPathTemplate = "/",
                            UpstreamHttpMethod = "Get",
                            AuthenticationOptions = new FileAuthenticationOptions
                            {
                                AdditionalScopes = new List<string>
                                {
                                    "openid", "offline_access"
                                },
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:52888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret",
                            },
                            AddHeadersToRequest =
                            {
                                {"CustomerId", "Claims[CustomerId] > value"},
                                {"LocationId", "Claims[LocationId] > value"},
                                {"UserType", "Claims[sub] > value[0] > |"},
                                {"UserId", "Claims[sub] > value[1] > |"}
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:52888", "api", AccessTokenType.Jwt, user))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:52876", 200))
                .And(x => _steps.GivenIHaveAToken("http://localhost:52888"))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("CustomerId: 123 LocationId: 1 UserType: registered UserId: 1231231"))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode)
        {
            _servicebuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var customerId = context.Request.Headers.First(x => x.Key == "CustomerId").Value.First();
                        var locationId = context.Request.Headers.First(x => x.Key == "LocationId").Value.First();
                        var userType = context.Request.Headers.First(x => x.Key == "UserType").Value.First();
                        var userId = context.Request.Headers.First(x => x.Key == "UserId").Value.First();

                        var responseBody = $"CustomerId: {customerId} LocationId: {locationId} UserType: {userType} UserId: {userId}";
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    });
                })
                .Build();

            _servicebuilder.Start();
        }

        private void GivenThereIsAnIdentityServerOn(string url, string scopeName, AccessTokenType tokenType, TestUser user)
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
                    .AddTemporarySigningCredential()
                        .AddInMemoryApiResources(new List<ApiResource>
                        {
                            new ApiResource
                            {
                                Name = scopeName,
                                Description = "My API",
                                Enabled = true,
                                DisplayName = "test",
                                Scopes = new List<Scope>()
                                {
                                    new Scope("api"),
                                    new Scope("openid"),
                                    new Scope("offline_access")
                                },
                                ApiSecrets = new List<Secret>()
                                {
                                    new Secret
                                    {
                                        Value = "secret".Sha256()
                                    }
                                },
                                UserClaims = new List<string>()
                                {
                                    "CustomerId", "LocationId", "UserType", "UserId"
                                }
                            }
                        })
                        .AddInMemoryClients(new List<Client>
                        {
                            new Client
                            {
                                ClientId = "client",
                                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                                ClientSecrets = new List<Secret> {new Secret("secret".Sha256())},
                                AllowedScopes = new List<string> { scopeName, "openid", "offline_access" },
                                AccessTokenType = tokenType,
                                Enabled = true,
                                RequireClientSecret = false
                            }
                        })
                        .AddTestUsers(new List<TestUser>
                        {
                            user
                        });
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                })
                .Build();

            _identityServerBuilder.Start();

            _steps.VerifyIdentiryServerStarted(url);
        }

        public void Dispose()
        {
            _servicebuilder?.Dispose();
            _steps.Dispose();
            _identityServerBuilder?.Dispose();
        }
    }
}
