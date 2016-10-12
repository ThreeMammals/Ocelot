using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Ocelot.Library.Infrastructure.Configuration.Yaml;
using Shouldly;
using TestStack.BDDfy;
using Xunit;
using YamlDotNet.Serialization;

namespace Ocelot.AcceptanceTests
{
    public class OcelotTests : IDisposable
    {
        private TestServer _server;
        private HttpClient _client;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private StringContent _postContent;
        private IWebHost _builder;

        public OcelotTests()
        {
            _configurationPath = "./bin/Debug/netcoreapp1.0/configuration.yaml";
        }

        [Fact]
        public void should_return_response_404_when_no_configuration_at_all()
        {
            this.Given(x => x.GivenThereIsAConfiguration(new YamlConfiguration()))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_simple_url()
        {
            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", 200, "Hello from Laura"))
                .And(x => x.GivenThereIsAConfiguration(new YamlConfiguration
                {
                    ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Get"
                        }
                    }
                }))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheResponseBodyShouldBe("Hello from Laura"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_with_complex_url()
        {
            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879/api/products/1", 200, "Some Product"))
                .And(x => x.GivenThereIsAConfiguration(new YamlConfiguration
                {
                    ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/api/products/{productId}",
                            UpstreamTemplate = "/products/{productId}",
                            UpstreamHttpMethod = "Get"
                        }
                    }
                }))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGateway("/products/1"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(x => x.ThenTheResponseBodyShouldBe("Some Product"))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_201_with_simple_url()
        {
            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", 201, string.Empty))
                .And(x => x.GivenThereIsAConfiguration(new YamlConfiguration
                {
                    ReRoutes = new List<YamlReRoute>
                    {
                        new YamlReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/",
                            UpstreamTemplate = "/",
                            UpstreamHttpMethod = "Post"
                        }
                    }
                }))
                .And(x => x.GivenTheApiGatewayIsRunning())
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

        private void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _client.GetAsync(url).Result;
        }

        private void WhenIPostUrlOnTheApiGateway(string url)
        {
            _response = _client.PostAsync(url, _postContent).Result;
        }

        private void ThenTheStatusCodeShouldBe(HttpStatusCode expectedHttpStatusCode)
        {
            _response.StatusCode.ShouldBe(expectedHttpStatusCode);
        }

        private void ThenTheResponseBodyShouldBe(string expectedBody)
        {
            _response.Content.ReadAsStringAsync().Result.ShouldBe(expectedBody);
        }

        public void Dispose()
        {
            if (_builder != null)
            {
                _builder.Dispose();
            }
            _client.Dispose();
            _server.Dispose();
        }
    }
}
