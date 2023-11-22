using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests
{
    public class DefaultVersionPolicyTests : IDisposable
    {
        private readonly Steps _steps;
        private const string Body = "supercalifragilistic";

        public DefaultVersionPolicyTests()
        {
            _steps = new Steps();
        }

        [Fact]
        public async Task should_return_bad_gateway_when_request_higher_receive_lower()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "2.0",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionOrHigher,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http1))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_bad_gateway_when_request_lower_receive_higher()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "1.1",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionOrLower,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_bad_gateway_when_request_exact_receive_different()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "1.1",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionExact,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.BadGateway))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_ok_when_request_version_exact_receive_exact()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "2.0",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionExact,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_ok_when_request_version_lower_receive_lower()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "2.0",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionOrLower,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http1))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_ok_when_request_version_lower_receive_exact()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "2.0",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionOrLower,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_ok_when_request_version_higher_receive_higher()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "1.1",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionOrHigher,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http2))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        [Fact]
        public async Task should_return_ok_when_request_version_higher_receive_exact()
        {
            var port = PortFinder.GetRandomPort();

            var configuration = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "https",
                        DownstreamHttpVersion = "1.1",
                        DownstreamVersionPolicy = VersionPolicies.RequestVersionOrHigher,
                        UpstreamPathTemplate = "/",
                        UpstreamHttpMethod = new List<string> { "GET" },
                    },
                },
            };

            this.Given(x => x.GivenThereIsAServiceRunningOn($"https://localhost:{port}", HttpProtocols.Http1))
                .And(x => _steps.GivenThereIsAConfiguration(configuration))
                .And(x => _steps.GivenOcelotIsRunning())
                .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
                .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
                .BDDfy();
        }

        private void GivenThereIsAServiceRunningOn(string url, HttpProtocols protocols)
        {
            var builder = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .ConfigureKestrel(serverOptions =>
                {
                    serverOptions.ConfigureEndpointDefaults(listenOptions => { listenOptions.Protocols = protocols; });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteAsync(Body);
                    });
                })
                .Build();

            builder.Start();
        }

        public void Dispose()
        {
            _steps?.Dispose();
        }
    }
}
