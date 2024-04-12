using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Text;

namespace Ocelot.AcceptanceTests.Request;

[Trait("PR", "1972")]
public sealed class RequestMapperTests : Steps, IDisposable
{
    private readonly ServiceHandler _serviceHandler;

    public RequestMapperTests()
    {
        _serviceHandler = new();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_map_request_without_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(";;"))
            .BDDfy();
    }

    [Fact]
    public void Should_map_request_with_content_length()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent("This is some content")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("20;;This is some content"))
            .BDDfy();
    }

    [Fact]
    public void Should_map_request_with_empty_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StringContent("")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("0;;"))
            .BDDfy();
    }

    [Fact]
    public void Should_map_request_with_chunked_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new ChunkedContent("This ", "is some content")))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(";chunked;This is some content"))
            .BDDfy();
    }

    [Fact]
    public void Should_map_request_with_empty_chunked_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/", HttpStatusCode.OK))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new ChunkedContent()))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(";chunked;"))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, HttpStatusCode status)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            var request = context.Request;
            var response = context.Response;
            response.StatusCode = (int)status;

            await response.WriteAsync(request.ContentLength + ";" + request.Headers.TransferEncoding + ";");
            await request.Body.CopyToAsync(response.Body);
        });
    }

    private static FileRoute GivenRoute(int port, string method = null) => new()
    {
        DownstreamPathTemplate = "/",
        DownstreamScheme = Uri.UriSchemeHttp,
        DownstreamHostAndPorts = new()
        {
            new("localhost", port),
        },
        UpstreamPathTemplate = "/",
        UpstreamHttpMethod = new() { method ?? HttpMethods.Get },
    };
}

internal class ChunkedContent : HttpContent
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
