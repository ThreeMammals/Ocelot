using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using IdentityServer4.Models;
using IdentityServer4.Services.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.Library.Infrastructure.Configuration.Yaml;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using YamlDotNet.Serialization;

namespace Ocelot.AcceptanceTests
{
    public class AuthenticationTests : IDisposable
    {
        private TestServer _ocelotServer;
        private HttpClient _ocelotClient;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private StringContent _postContent;
        private IWebHost _ocelotBbuilder;

        // Sadly we need to change this when we update the netcoreapp version to make the test update the config correctly
        private double _netCoreAppVersion = 1.4;
        private BearerToken _token;
        private IWebHost _identityServerBuilder;

        public AuthenticationTests()
        {
            _configurationPath = $"./bin/Debug/netcoreapp{_netCoreAppVersion}/configuration.yaml";
        }

        [Fact]
        public void should_return_401_using_identity_server_access_token()
        {
            this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:51888"))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51876", 201, string.Empty))
                .And(x => x.GivenThereIsAConfiguration(new YamlConfiguration
                {
                    ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51876/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post",
                            Authentication = "JwtBearerAuthentication"
                        }
                    }
                }))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .And(x => x.GivenThePostHasContent("postContent"))
                .When(x => x.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.Unauthorized))
                .BDDfy();
        }

        [Fact]
        public void should_return_201_using_identity_server_access_token()
        {
            this.Given(x => x.GivenThereIsAnIdentityServerOn("http://localhost:51888"))
                .And(x => x.GivenThereIsAServiceRunningOn("http://localhost:51876", 201, string.Empty))
                .And(x => x.GivenIHaveAToken("http://localhost:51888"))
                .And(x => x.GivenThereIsAConfiguration(new YamlConfiguration
                {
                    ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51876/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post",
                            Authentication = "JwtBearerAuthentication"
                        }
                    }
                }))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .And(x => x.GivenIHaveAddedATokenToMyRequest())
                .And(x => x.GivenThePostHasContent("postContent"))
                .When(x => x.WhenIPostUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.Created))
                .BDDfy();
        }

        private void GivenThePostHasContent(string postcontent)
        {
            _postContent = new StringContent(postcontent);
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

        private void GivenThereIsAServiceRunningOn(string url, int statusCode, string responseBody)
        {
            _ocelotBbuilder = new WebHostBuilder()
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

            _ocelotBbuilder.Start();
        }

        private void GivenThereIsAnIdentityServerOn(string url)
        {
            _identityServerBuilder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .ConfigureServices(services =>
                {
                    services.AddDeveloperIdentityServer()
                    .AddInMemoryScopes(new List<Scope> { new Scope
                    {
                        Name = "api",
                        Description = "My API",
                        Enabled = true

                    }})
                    .AddInMemoryClients(new List<Client> {
                    new Client
                    {
                        ClientId = "client",
                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                        ClientSecrets = new List<Secret> {  new Secret("secret".Sha256()) },
                        AllowedScopes = new List<string> { "api" },
                        AccessTokenType = AccessTokenType.Jwt,
                        Enabled = true,
                        RequireClientSecret = false
                    } })
                    .AddInMemoryUsers(new List<InMemoryUser> { new InMemoryUser
                    {
                        Username = "test",
                        Password = "test",
                        Enabled = true,
                        Subject = "asdads"
                    }});
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

        private void WhenIPostUrlOnTheApiGateway(string url)
        {
            _response = _ocelotClient.PostAsync(url, _postContent).Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            _ocelotBbuilder?.Dispose();
            _ocelotClient?.Dispose();
            _ocelotServer?.Dispose();
            _identityServerBuilder?.Dispose();
        }

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
