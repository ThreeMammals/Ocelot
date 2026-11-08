using Microsoft.AspNetCore.Http;
using System.Text;

namespace Ocelot.Acceptance.Request;

[Trait("PR", "1972")] // https://github.com/ThreeMammals/Ocelot/pull/1972
public sealed class RequestMapperTests : Steps
{
    [Fact]
    public async Task Should_map_request_without_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(";;");
    }

    [Fact]
    public async Task Should_map_request_with_content_length()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", new StringContent("This is some content"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("20;;This is some content");
    }

    [Fact]
    public async Task Should_map_request_with_empty_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", new StringContent(""));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("0;;");
    }

    [Fact]
    [Trait("Feat", "928")] // https://github.com/ThreeMammals/Ocelot/issues/928
    public async Task Should_map_request_with_chunked_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", new ChunkedContent("This ", "is some content"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(";chunked;This is some content");
    }

    [Fact]
    [Trait("Feat", "928")] // https://github.com/ThreeMammals/Ocelot/issues/928
    public async Task Should_map_request_with_empty_chunked_content()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, null);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", new ChunkedContent());
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync(";chunked;");
    }

    protected override async Task MapStatus(HttpContext context)
    {
        var request = context.Request;
        var response = context.Response;
        response.StatusCode = StatusCodes.Status200OK;
        await response.WriteAsync(request.ContentLength + ";" + request.Headers.TransferEncoding + ";");
        await request.Body.CopyToAsync(response.Body);
    }
}

internal class ChunkedContent : HttpContent
{
    private readonly string[] _chunks;
    public ChunkedContent(params string[] chunks) => _chunks = chunks;

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
