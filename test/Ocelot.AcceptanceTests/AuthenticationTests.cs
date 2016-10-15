using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using IdentityServer4.Models;
using IdentityServer4.Services.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Library.Infrastructure.Configuration.Yaml;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using YamlDotNet.Serialization;

namespace Ocelot.AcceptanceTests
{
    public class AuthenticationTests : IDisposable
    {
        private TestServer _server;
        private HttpClient _client;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private StringContent _postContent;
        private IWebHost _builder;

        // Sadly we need to change this when we update the netcoreapp version to make the test update the config correctly
        private double _netCoreAppVersion = 1.4;
        private HttpClient _idServerClient;
        private TestServer _idServer;

        public AuthenticationTests()
        {
            _configurationPath = $"./bin/Debug/netcoreapp{_netCoreAppVersion}/configuration.yaml";
        }

        [Fact]
        public void should_return_401_using_jwt()
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

        private void GivenThePostHasContent(string postcontent)
        {
            _postContent = new StringContent(postcontent);
        }

        /// <summary>
        /// This is annoying cos it should be in the constructor but we need to set up the yaml file before calling startup so its a step.
        /// </summary>
        private void GivenTheApiGatewayIsRunning()
        {
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());

            _client = _server.CreateClient();
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
            _builder = new WebHostBuilder()
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

            _builder.Start();
        }

        private void GivenThereIsAnIdentityServerOn(string url)
        {
            var builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .ConfigureServices(services =>
                {
                    services.AddDeveloperIdentityServer()
                    .AddInMemoryClients(new List<Client> {
                    new Client
                    {
                        ClientId = "test",
                        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                        ClientSecrets = new List<Secret> {  new Secret("test".Sha256()) },
                        AllowedScopes = new List<string> { "api1" },
                        AllowAccessToAllScopes = true,
                        AccessTokenType = AccessTokenType.Jwt,
                        Enabled = true

                    } })
                    .AddInMemoryScopes(new List<Scope> { new Scope
                    {
                        Name = "api1",
                        Description = "My API",
                        Enabled = true

                    }})
                    .AddInMemoryUsers(new List<InMemoryUser> { new InMemoryUser
                    {
                        Username = "test", Password = "test", Enabled = true, Subject = "asdads"
                    }});
                })
                .Configure(app =>
                {
                    app.UseIdentityServer();
                });
                
            _idServer = new TestServer(builder);
            _idServerClient = _idServer.CreateClient();

            var response = _idServerClient.GetAsync($"{url}/.well-known/openid-configuration").Result;
            response.EnsureSuccessStatusCode();
            var content = response.Content.ReadAsStringAsync().Result;
        }

        private void GivenIHaveAToken(string url)
        {
            var tokenUrl = $"{url}/connect/token";
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", "test"),
                new KeyValuePair<string, string>("client_secret", "test".Sha256()),
                new KeyValuePair<string, string>("scope", "api1"),
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "test"),
                new KeyValuePair<string, string>("grant_type", "password")
            };
            var content = new FormUrlEncodedContent(formData);
            var response = _idServerClient.PostAsync(tokenUrl, content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
        }

        private void WhenIPostUrlOnTheApiGateway(string url)
        {
            _response = _client.PostAsync(url, _postContent).Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        public void Dispose()
        {
            _idServerClient?.Dispose();
            _idServer?.Dispose();
            _builder?.Dispose();
            _client.Dispose();
            _server.Dispose();
        }
    }
}
