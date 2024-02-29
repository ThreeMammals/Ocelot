using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Text;

namespace Ocelot.AcceptanceTests;

public class MapRequestTests : IDisposable
{
    private readonly ServiceHandler _serviceHandler;
    private readonly Steps _steps;

    private class ChunkedContent : HttpContent
    {
        private readonly string[] _chunks;

        public ChunkedContent(params string[] chunks)
        {
            _chunks = chunks;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            foreach (var chunk in _chunks)
            {
                var bytes = Encoding.Default.GetBytes(chunk);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }

    public MapRequestTests()
    {
        _serviceHandler = new ServiceHandler();
        _steps = new Steps();
    }
    
    [Fact]
    public void should_map_request_without_content()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(";;"))
            .BDDfy();
    }

    [Fact]
    public void should_map_request_with_content_length()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Post" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent("This is some content")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("20;;This is some content"))
            .BDDfy();
    }

    [Fact]
    public void should_map_request_with_empty_content()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Post" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StringContent("")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe("0;;"))
            .BDDfy();
    }

    [Fact]
    public void should_map_request_with_chunked_content()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Post" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new ChunkedContent("This ", "is some content")))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(";chunked;This is some content"))
            .BDDfy();
    }

    [Fact]
    public void should_map_request_with_empty_chunked_content()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamScheme = "http",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Post" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new ChunkedContent()))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(";chunked;"))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            var request = context.Request;
            var response = context.Response;

            await response.WriteAsync(request.ContentLength + ";" + request.Headers.TransferEncoding + ";");
            await request.Body.CopyToAsync(response.Body);
        });
    }

    public void Dispose()
    {
        _serviceHandler?.Dispose();
        _steps?.Dispose();
    }
}
