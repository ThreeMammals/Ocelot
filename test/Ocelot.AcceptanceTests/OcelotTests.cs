namespace Ocelot.AcceptanceTests
{
    using System;
    using System.Net.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Xunit;
    using Ocelot.AcceptanceTests.Fake;
    using Shouldly;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using Library.Infrastructure.Configuration;
    using TestStack.BDDfy;
    using YamlDotNet.Serialization;

    public class OcelotTests : IDisposable
    {
        private readonly FakeService _fakeService;
        private TestServer _server;
        private HttpClient _client;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private string _postContent;

        public OcelotTests()
        {
            _configurationPath = "./bin/Debug/netcoreapp1.0/configuration.yaml";
            _fakeService = new FakeService();
        }

        [Fact]
        public void should_return_response_404()
        {
            this.Given(x => x.GivenThereIsAConfiguration(new Configuration()))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200()
        {
            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => x.GivenThereIsAConfiguration(new Configuration
                {
                    ReRoutes = new List<ReRoute>
                    {
                        new ReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/",
                            UpstreamTemplate = "/"
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
        public void should_return_response_201()
        {
            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879"))
                .And(x => x.GivenThereIsAConfiguration(new Configuration
                {
                    ReRoutes = new List<ReRoute>
                    {
                        new ReRoute
                        {
                            DownstreamTemplate = "http://localhost:51879/",
                            UpstreamTemplate = "/"
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
            _postContent = postcontent;
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

        private void GivenThereIsAConfiguration(Configuration configuration)
        {
            var serializer = new Serializer();

            if (File.Exists(_configurationPath))
            {
                File.Delete(_configurationPath);
            }

            using (TextWriter writer = File.CreateText(_configurationPath))
            {
                serializer.Serialize(writer, configuration);
            }
        }

        private void GivenThereIsAServiceRunningOn(string url)
        {
            _fakeService.Start(url);
        }

        private void WhenIGetUrlOnTheApiGateway(string url)
        {
            _response = _client.GetAsync(url).Result;
        }

        private void WhenIPostUrlOnTheApiGateway(string url)
        {
            var content = new StringContent(_postContent);
            _response = _client.PostAsync(url, content).Result;
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
            _fakeService.Stop();
            _client.Dispose();
            _server.Dispose();
        }
    }
}
