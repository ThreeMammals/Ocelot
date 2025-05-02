using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public sealed class MethodTests : Steps
{
    private readonly ServiceHandler _serviceHandler;

    public MethodTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    [Fact]
    public void Should_return_response_200_when_get_converted_to_post()
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
                    },
                },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", "POST"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_get_converted_to_post_with_content()
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
                },
            },
        };

        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", "POST"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    [Fact]
    public void Should_return_response_200_when_get_converted_to_get_with_content()
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
                    UpstreamHttpMethod = new List<string> { "Post" },
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamHttpMethod = "GET",
                },
            },
        };

        const string expected = "here is some content";
        var httpContent = new StringContent(expected);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", "GET"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", httpContent))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheResponseBodyShouldBe(expected))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, string expected)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            if (context.Request.Method == expected)
            {
                context.Response.StatusCode = 200;
                var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();
                await context.Response.WriteAsync(body);
            }
            else
            {
                context.Response.StatusCode = 500;
            }
        });
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }
}
