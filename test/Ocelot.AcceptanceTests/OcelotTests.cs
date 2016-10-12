namespace Ocelot.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Ocelot.Library.Infrastructure.Configuration.Yaml;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using YamlDotNet.Serialization;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    public class OcelotTests : IDisposable
    {
        private TestServer _server;
        private HttpClient _client;
        private HttpResponseMessage _response;
        private readonly string _configurationPath;
        private StringContent _postContent;
        private Task _fake;

        public OcelotTests()
        {
            _configurationPath = "./bin/Debug/netcoreapp1.0/configuration.yaml";
        }

        [Fact]
        public void should_return_response_404()
        {
            this.Given(x => x.GivenThereIsAConfiguration(new YamlConfiguration()))
                .And(x => x.GivenTheApiGatewayIsRunning())
                .When(x => x.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => x.ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200()
        {
            var serviceResponse = new DefaultHttpContext();
            serviceResponse.Request.Method = "get";
            serviceResponse.Response.Body = GenerateStreamFromString("Hello from Laura");
            serviceResponse.Response.StatusCode = 200;

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", serviceResponse))
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
        public void should_return_response_201()
        {
            var serviceResponse = new DefaultHttpContext();
            serviceResponse.Request.Method = "post";
            serviceResponse.Response.StatusCode = 201;

            this.Given(x => x.GivenThereIsAServiceRunningOn("http://localhost:51879", serviceResponse))
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

        public Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
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

        private void GivenThereIsAServiceRunningOn(string url, HttpContext httpContext)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.Body = httpContext.Response.Body;
                        context.Response.StatusCode = httpContext.Response.StatusCode;

                       /* if (context.Request.Method.ToLower() == "get")
                        {
                            await context.Response.WriteAsync("Hello from Laura");
                        }
                        else
                        {
                            context.Response.StatusCode = 201;
                        }*/
                    });
                })
                .UseUrls(url)
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseUrls(url)
                .Build();

            _fake = Task.Run(() => builder.Run());
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
            _client.Dispose();
            _server.Dispose();
        }
    }
}
