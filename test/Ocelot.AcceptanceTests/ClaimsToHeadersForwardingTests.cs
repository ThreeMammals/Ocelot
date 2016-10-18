using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using IdentityServer4.Models;
using IdentityServer4.Services.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Library.Configuration.Yaml;
using Ocelot.ManualTest;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using YamlDotNet.Serialization;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Ocelot.AcceptanceTests
{
    public class ClaimsToHeadersForwardingTests : IDisposable
    {
        private TestServer _ocelotServer;
        private HttpClient _ocelotClient;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private IWebHost _servicebuilder;

        // Sadly we need to change this when we update the netcoreapp version to make the test update the config correctly
        private double _netCoreAppVersion = 1.4;
        private BearerToken _token;
        private IWebHost _identityServerBuilder;

        public ClaimsToHeadersForwardingTests()
        {
            _configurationPath = $"./bin/Debug/netcoreapp{_netCoreAppVersion}/configuration.yaml";
        }

        [Fact]
        public void should_return_response_200_and_foward_claim_as_header()
        {
            var user = new InMemoryUser
            {
                Username = "test",
                Password = "test",
                Enabled = true,
                Subject = "registered|1231231",
                Claims = new List<Claim>
                {
                    new Claim("CustomerId", "123"),
                    new Claim("LocationId", "1")
                }
            };

            this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:52888", "api", AccessTokenType.Jwt, user))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:52876", 200))
                .And(x => x.GivenIHaveAToken("http://localhost:52888"))
                .And(x => x.GivenThereIsAConfiguration(new YamlConfiguration
                {
                    ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:52876/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get",
                            AuthenticationOptions = new YamlAuthenticationOptions
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
                }))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .And(x => x.GivenIHaveAddedATokenToMyRequest())
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheResponseBodyShouldBe("CustomerId: 123 LocationId: 1 UserType: registered UserId: 1231231"))
                .BDDfy();
        }

        private void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.GetAsync(url).Result;
        }

        private void ThenTheResponseBodyShouldBe(string expectedBody)
        {
            _response.Content.ReadAsStringAsync().Result.ShouldBe(expectedBody);
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the yaml file before calling startup so its a step.
        /// </summary>
        private void GivenTheApiGatewayIsRunning()
        {
            _ocelotServer = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());

            _ocelotClient = _ocelotServer.CreateClient();
        }

        private void GivenThereIsAConfiguration(YamlConfiguration yamlConfiguration)
        {
            var serializer = new Serializer();

            if (File.Exists(_configurationPath))
            {
                File.Delete(_configurationPath);
            }

            using (TextWriter writer = File.CreateText(_configurationPath))
            {
                serializer.Serialize(writer, yamlConfiguration);
            }
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

        private void GivenThereIsAnIdentityServerOn(string url, string scopeName, AccessTokenType tokenType, InMemoryUser user)
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
                            user
                        });
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                })
                .Build();

            _identityServerBuilder.Start();

            VerifyIdentiryServerStarted(url);

        }

        private void VerifyIdentiryServerStarted(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync($"{url}/.well-known/openid-configuration").Result;
                response.EnsureSuccessStatusCode();
            }
        }

        private void GivenIHaveAToken(string url)
        {
            var tokenUrl = $"{url}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "client"),
                new KeyValuePair<string, string>("client_secret", "secret"),
                new KeyValuePair<string, string>("scope", "api"),
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "test"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);

            using (var httpClient = new HttpClient())
            {
                var response = httpClient.PostAsync(tokenUrl, content).Result;
                response.EnsureSuccessStatusCode();
                var responseContent = response.Content.ReadAsStringAsync().Result;
                _token = JsonConvert.DeserializeObject<BearerToken>(responseContent);
            }
        }

        private void GivenIHaveAddedATokenToMyRequest()
        {
            _ocelotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            _servicebuilder?.Dispose();
            _ocelotClient?.Dispose();
            _ocelotServer?.Dispose();
            _identityServerBuilder?.Dispose();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        class BearerToken
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }
        }
    }
}
