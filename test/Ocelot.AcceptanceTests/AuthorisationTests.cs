using System;
using System.Collections.Generic;
using System.IO;
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

namespace Ocelot.AcceptanceTests
{
    using IdentityServer4;
    using IdentityServer4.Test;

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
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = "/",
                           DownstreamPort = 51876,
                           DownstreamScheme = "http",
                           DownstreamHost = "localhost",
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Get" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                            AllowedScopes =  new List<string>(),
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = "http://localhost:51888",
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                               }
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
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
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
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = "/",
                           DownstreamPort = 51876,
                           DownstreamScheme = "http",
                           DownstreamHost = "localhost",
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Get" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                                AllowedScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                IdentityServerConfig = new FileIdentityServerConfig{
                                        ProviderRootUrl = "http://localhost:51888",
                                        RequireHttps = false,
                                        ApiName = "api",
                                        ApiSecret = "secret"
                                }   
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
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
               .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Forbidden))
               .BDDfy();
        }

        [Fact]
        public void should_return_response_200_using_identity_server_with_allowed_scope()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = "/",
                           DownstreamPort = 51876,
                           DownstreamHost = "localhost",
                           DownstreamScheme = "http",
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Get" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes =  new List<string>{ "api", "api.readOnly", "openid", "offline_access" },
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = "http://localhost:51888",
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                                }   
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:51888", "api", AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51876", 200, "Hello from Laura"))
               .And(x => _steps.GivenIHaveATokenForApiReadOnlyScope("http://localhost:51888"))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
               .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
               .BDDfy();
        }

        [Fact]
        public void should_return_response_403_using_identity_server_with_scope_not_allowed()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = "/",
                           DownstreamPort = 51876,
                           DownstreamHost = "localhost",
                           DownstreamScheme = "http",
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Get" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes =  new List<string>{ "api", "openid", "offline_access" },
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                        ProviderRootUrl = "http://localhost:51888",
                                        RequireHttps = false,
                                        ApiName = "api",
                                        ApiSecret = "secret"
                                }
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:51888", "api", AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51876", 200, "Hello from Laura"))
               .And(x => _steps.GivenIHaveATokenForApiReadOnlyScope("http://localhost:51888"))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
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

        private void GivenThereIsAnIdentityServerOn(string url, string apiName, AccessTokenType tokenType)
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
                                Name = apiName,
                                Description = "My API",
                                Enabled = true,
                                DisplayName = "test",
                                Scopes = new List<Scope>()
                                {
                                    new Scope("api"),
                                    new Scope("api.readOnly"),
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
                            },

                        })
                        .AddInMemoryClients(new List<Client>
                        {
                            new Client
                            {
                                ClientId = "client",
                                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                                ClientSecrets = new List<Secret> {new Secret("secret".Sha256())},
                                AllowedScopes = new List<string> { apiName, "api.readOnly", "openid", "offline_access" },
                                AccessTokenType = tokenType,
                                Enabled = true,
                                RequireClientSecret = false
                            }
                        })
                        .AddTestUsers(new List<TestUser>
                        {
                            new TestUser
                            {
                                Username = "test",
                                Password = "test",
                                SubjectId = "registered|1231231",
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
