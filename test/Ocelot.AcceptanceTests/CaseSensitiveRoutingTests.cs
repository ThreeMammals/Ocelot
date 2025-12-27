using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class CaseSensitiveRoutingTests : Steps
{
    public CaseSensitiveRoutingTests()
    {
    }

    [Fact]
    public void Should_return_response_200_when_global_ignore_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/products/{productId}",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = ["Get"],
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", 200, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_route_ignore_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/products/{productId}",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = ["Get"],
                        RouteIsCaseSensitive = false,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", 200, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_404_when_route_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/products/{productId}",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = ["Get"],
                        RouteIsCaseSensitive = true,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", 200, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_route_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/products/{productId}",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/PRODUCTS/{productId}",
                        UpstreamHttpMethod = ["Get"],
                        RouteIsCaseSensitive = true,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", 200, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_404_when_global_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/products/{productId}",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/products/{productId}",
                        UpstreamHttpMethod = ["Get"],
                        RouteIsCaseSensitive = true,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", 200, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.NotFound))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_global_respect_case_sensitivity_set()
    {
        var port = PortFinder.GetRandomPort();
        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
                {
                    new()
                    {
                        DownstreamPathTemplate = "/api/products/{productId}",
                        DownstreamHostAndPorts = new List<FileHostAndPort>
                        {
                            new()
                            {
                                Host = "localhost",
                                Port = port,
                            },
                        },
                        DownstreamScheme = "http",
                        UpstreamPathTemplate = "/PRODUCTS/{productId}",
                        UpstreamHttpMethod = ["Get"],
                        RouteIsCaseSensitive = true,
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/api/products/1", 200, "Some Product"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/PRODUCTS/1"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, int statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(responseBody);
        });
    }
}
