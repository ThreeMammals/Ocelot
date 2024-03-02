using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.File;
using System.Security.Authentication;

namespace Ocelot.AcceptanceTests
{
    public class HttpTests : IDisposable
    {
        private readonly Steps _steps;
        private readonly ServiceHandler _serviceHandler;

        public HttpTests()
        {
            _serviceHandler = new ServiceHandler();
            _steps = new Steps();
        }

        [Fact]
        public void should_return_response_200_when_using_http_one()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/{url}",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamHttpMethod = "POST",
                        DownstreamHttpVersion = "1.0",
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", port, HttpProtocols.Http1))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_using_http_one_point_one()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                    {
                        new()
                        {
                            DownstreamPathTemplate = "/{url}",
                            DownstreamScheme = "http",
                            UpstreamPathTemplate = "/{url}",
                            UpstreamHttpMethod = new List<string> { "Get" },
                            DownstreamHostAndPorts = new List<FileHostAndPort>
                            {
                                new()
                                {
                                    Host = "localhost",
                                    Port = port,
                                },
                            },
                            DownstreamHttpMethod = "POST",
                            DownstreamHttpVersion = "1.1",
                        },
                    },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", port, HttpProtocols.Http1))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_200_when_using_http_two_point_zero()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/{url}",
                        DownstreamScheme = "https",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamHttpMethod = "POST",
                        DownstreamHttpVersion = "2.0",
                        DangerousAcceptAnyServerCertificateValidator = true,
                    },
                },
            };

            const string expected = "here is some content";
            var httpContent = new StringContent(expected);

            this.Given(x => x.GivenThereIsAServiceUsingHttpsRunningOn($"http://localhost:{port}/", "/", port, HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/", httpContent))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(_ => _steps.ThenTheResponseBodyShouldBe(expected))
                .BDDfy();
        }

        [Fact]
        public void should_return_response_502_when_using_http_one_to_talk_to_server_running_http_two()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/{url}",
                        DownstreamScheme = "https",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamHttpMethod = "POST",
                        DownstreamHttpVersion = "1.1",
                        DangerousAcceptAnyServerCertificateValidator = true,
                    },
                },
            };

            const string expected = "here is some content";
            var httpContent = new StringContent(expected);

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", port, HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/", httpContent))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
                .BDDfy();
        }

        //TODO: does this test make any sense?
        [Fact]
        public void should_return_response_200_when_using_http_two_to_talk_to_server_running_http_one_point_one()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/{url}",
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/{url}",
                        UpstreamHttpMethod = new List<string> { "Get" },
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamHttpMethod = "POST",
                        DownstreamHttpVersion = "1.1",
                        DangerousAcceptAnyServerCertificateValidator = true,
                    },
                },
            };

            const string expected = "here is some content";
            var httpContent = new StringContent(expected);

            this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}/", "/", port, HttpProtocols.Http1))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/", httpContent))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .And(_ => _steps.ThenTheResponseBodyShouldBe(expected))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int port, HttpProtocols protocols)
        {
            void options(KestrelServerOptions serverOptions)
            {
                serverOptions.Listen(IPAddress.Loopback, port, listenOptions =>
                {
                    listenOptions.Protocols = protocols;
                });
            }

            _serviceHandler.GivenThereIsAServiceRunningOnWithKestrelOptions(baseUrl, basePath, options, async context =>
            {
                context.Response.StatusCode = 200;
                var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                await context.Response.WriteAsync(body);
            });
        }

        private void GivenThereIsAServiceUsingHttpsRunningOn(string baseUrl, string basePath, int port, HttpProtocols protocols)
        {
            void options(KestrelServerOptions serverOptions)
            {
                serverOptions.Listen(IPAddress.Loopback, port, listenOptions =>
                {
                    listenOptions.UseHttps("mycert.pfx", "password", options =>
                    {
                        options.SslProtocols = SslProtocols.Tls12;
                    });
                    listenOptions.Protocols = protocols;
                });
            }

            _serviceHandler.GivenThereIsAServiceRunningOnWithKestrelOptions(baseUrl, basePath, options, async context =>
            {
                context.Response.StatusCode = 200;
                var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                await context.Response.WriteAsync(body);
            });
        }

        public void Dispose()
        {
            _serviceHandler.Dispose();
            _steps.Dispose();
        }
    }
}
