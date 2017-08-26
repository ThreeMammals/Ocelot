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

    public class AuthenticationTests : IDisposable
    {
        private IWebHost _servicebuilder;
        private readonly Steps _steps;
        private IWebHost _identityServerBuilder;
        private string _identityServerRootUrl = "http://localhost:51888";
        private string _downstreamServicePath = "/";
        private string _downstreamServiceHost = "localhost";
        private int _downstreamServicePort = 51876;
        private string _downstreamServiceScheme = "http";
        private string _downstreamServiceUrl = "http://localhost:51876";

        public AuthenticationTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_401_using_identity_server_access_token()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = _downstreamServicePath,
                           DownstreamPort = _downstreamServicePort,
                           DownstreamHost = _downstreamServiceHost,
                           DownstreamScheme = _downstreamServiceScheme,
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Post" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes =  new List<string>(),
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = _identityServerRootUrl,
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                               }                               
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceUrl, 201, string.Empty))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenThePostHasContent("postContent"))
               .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
               .BDDfy();
        }

        [Fact]
        public void should_return_response_200_using_identity_server()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = _downstreamServicePath,
                           DownstreamPort = _downstreamServicePort,
                           DownstreamHost = _downstreamServiceHost,
                           DownstreamScheme = _downstreamServiceScheme,
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Get" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes =  new List<string>(),
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = _identityServerRootUrl,
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                               }
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceUrl, 200, "Hello from Laura"))
               .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
               .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
               .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
               .BDDfy();
        }

        [Fact]
        public void should_return_response_401_using_identity_server_with_token_requested_for_other_api()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = _downstreamServicePath,
                           DownstreamPort = _downstreamServicePort,
                           DownstreamHost = _downstreamServiceHost,
                           DownstreamScheme = _downstreamServiceScheme,
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Get" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes =  new List<string>(),
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = _identityServerRootUrl,
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                               }
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceUrl, 200, "Hello from Laura"))
               .And(x => _steps.GivenIHaveATokenForApi2(_identityServerRootUrl))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
               .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
               .BDDfy();
        }

        [Fact]
        public void should_return_201_using_identity_server_access_token()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = _downstreamServicePath,
                           DownstreamPort = _downstreamServicePort,
                           DownstreamHost = _downstreamServiceHost,
                           DownstreamScheme = _downstreamServiceScheme,
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Post" },

                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes =  new List<string>(),
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = _identityServerRootUrl,
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                               }
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Jwt))
               .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceUrl, 201, string.Empty))
               .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
               .And(x => _steps.GivenThePostHasContent("postContent"))
               .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
               .BDDfy();
        }

        [Fact]
        public void should_return_201_using_identity_server_reference_token()
        {
           var configuration = new FileConfiguration
           {
               ReRoutes = new List<FileReRoute>
                   {
                       new FileReRoute
                       {
                           DownstreamPathTemplate = _downstreamServicePath,
                           DownstreamPort = _downstreamServicePort,
                           DownstreamHost = _downstreamServiceHost,
                           DownstreamScheme = _downstreamServiceScheme,
                           UpstreamPathTemplate = "/",
                           UpstreamHttpMethod = new List<string> { "Post" },
                           AuthenticationOptions = new FileAuthenticationOptions
                           {
                               AllowedScopes = new List<string>(),
                               Provider = "IdentityServer",
                               IdentityServerConfig = new FileIdentityServerConfig{
                                    ProviderRootUrl = _identityServerRootUrl,
                                    RequireHttps = false,
                                    ApiName = "api",
                                    ApiSecret = "secret"
                               }
                           }
                       }
                   }
           };

           this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", "api2", AccessTokenType.Reference))
               .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceUrl, 201, string.Empty))
               .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
               .And(x => _steps.GivenThereIsAConfiguration(configuration))
               .And(x => _steps.GivenOcelotIsRunning())
               .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
               .And(x => _steps.GivenThePostHasContent("postContent"))
               .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
               .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
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
                                    "CustomerId", "LocationId"
                                }
                            },
                            new ApiResource
                            {
                                Name = api2Name,
                                Description = "My second API",
                                Enabled = true,
                                DisplayName = "second test",
                                Scopes = new List<Scope>()
                                {
                                    new Scope("api2"),
                                    new Scope("api2.readOnly"),
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
                                    "CustomerId", "LocationId"
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
                                AllowedScopes = new List<string> { apiName, api2Name, "api.readOnly", "openid", "offline_access" },
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
