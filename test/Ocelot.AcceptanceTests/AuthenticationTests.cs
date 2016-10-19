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
    public class AuthenticationTests : IDisposable
    {
        private IWebHost _servicebuilder;
        private readonly Steps _steps;
        private IWebHost _identityServerBuilder;
        private string _identityServerRootUrl = "http://localhost:51888";
        private string _downstreamServiceRootUrl = "http://localhost:51876/";

        public AuthenticationTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public void should_return_401_using_identity_server_access_token()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = _downstreamServiceRootUrl,
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = _identityServerRootUrl,
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceRootUrl, 201, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
                .BDDfy();
        }

        [Fact]
        public void should_return_401_using_identity_server_reference_token()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = _downstreamServiceRootUrl,
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = _identityServerRootUrl,
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Reference))
                .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceRootUrl, 201, string.Empty))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenThePostHasContent("postContent"))
                .When(x => _steps.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_using_identity_server()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = _downstreamServiceRootUrl,
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = _identityServerRootUrl,
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceRootUrl, 200, "Hello from Laura"))
                .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
                .And(x => _steps.GivenOcelotIsRunning())
                .And(x => _steps.GivenIHaveAddedATokenToMyRequest())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => _steps.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_201_using_identity_server_access_token()
        {
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = _downstreamServiceRootUrl,
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes =  new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = _identityServerRootUrl,
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Jwt))
                .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceRootUrl, 201, string.Empty))
                .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
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
            var yamlConfiguration = new YamlConfiguration
            {
                ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = _downstreamServiceRootUrl,
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post",
                            AuthenticationOptions = new YamlAuthenticationOptions
                            {
                                AdditionalScopes = new List<string>(),
                                Provider = "IdentityServer",
                                ProviderRootUrl = _identityServerRootUrl,
                                RequireHttps = false,
                                ScopeName = "api",
                                ScopeSecret = "secret"
                            }
                        }
                    }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn(_identityServerRootUrl, "api", AccessTokenType.Reference))
                .And(x => x.GivenThereIsAServiceRunningOn(_downstreamServiceRootUrl, 201, string.Empty))
                .And(x => _steps.GivenIHaveAToken(_identityServerRootUrl))
                .And(x => _steps.GivenThereIsAConfiguration(yamlConfiguration))
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
                                }
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
