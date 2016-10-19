using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Services.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Yaml;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests
{
    public class AuthorisationTests : IDisposable
    {
        private IWebHost _servicebuilder;
        private IWebHost _identityServerBuilder;
        private readonly Steps _steps;

        public AuthorisationTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_authorising_route()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51876/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:51888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            },
                            AddHeadersToRequest =
                            {
                                {"CustomerId", "Claims[CustomerId] > value"},
                                {"LocationId", "Claims[LocationId] > value"},
                                {"UserType", "Claims[sub] > value[0] > |"},
                                {"UserId", "Claims[sub] > value[1] > |"}
                            },
                            AddClaimsToRequest =
                            {
                                {"CustomerId", "Claims[CustomerId] > value"},
                                {"UserType", "Claims[sub] > value[0] > |"},
                                {"UserId", "Claims[sub] > value[1] > |"}
                            },
                            RouteClaimsRequirement =
                            {
                                {"UserType", "registered"}
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:51888", "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51876", 200, "Hello from Laura"))
                .And(x => _steps.GivenIHaveAToken("http://localhost:51888"))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_403_authorising_route()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51876/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = "http://localhost:51888",
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            },
                            AddHeadersToRequest =
                            {
                                {"CustomerId", "Claims[CustomerId] > value"},
                                {"LocationId", "Claims[LocationId] > value"},
                                {"UserType", "Claims[sub] > value[0] > |"},
                                {"UserId", "Claims[sub] > value[1] > |"}
                            },
                            AddClaimsToRequest =
                            {
                                {"CustomerId", "Claims[CustomerId] > value"},
                                {"UserId", "Claims[sub] > value[1] > |"}
                            },
                            RouteClaimsRequirement =
                            {
                                {"UserType", "registered"}
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:51888", "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51876", 200, "Hello from Laura"))
                .And(x => _steps.GivenIHaveAToken("http://localhost:51888"))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
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
                        context.Response.StatusCode = statusCode;
                        await context.Response.WriteAsync(responseBody);
                    });
                })
                .Build();

            _servicebuilder.Start();
        }

        private void GivenThereIsAnIdentityServerOn(string url, string scopeName, AccessTokenType tokenType)
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
                    services.AddDeveloperIdentityServer()
                        .AddInMemoryScopes(new List<Scope>
                        {
                            new Scope
                            {
                                Name = scopeName,
                                Description = "My API",
                                Enabled = true,
                                AllowUnrestrictedIntrospection = true,
                                ScopeSecrets = new List<Secret>()
                                {
                                    new Secret
                                    {
                                        Value = "secret".Sha256()
                                    }
                                },
                                IncludeAllClaimsForUser = true
                            },

                            StandardScopes.OpenId,
                            StandardScopes.OfflineAccess
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
                        .AddInMemoryUsers(new List<InMemoryUser>
                        {
                            new InMemoryUser
                            {
                                Username = "test",
                                Password = "test",
                                Enabled = true,
                                Subject = "registered|1231231",
                                Claims = new List<Claim>
                                {
                                   new Claim("CustomerId", "123"),
                                   new Claim("LocationId", "321")
                                }
                            }
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
