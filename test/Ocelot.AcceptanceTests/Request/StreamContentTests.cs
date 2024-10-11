using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Ocelot.Configuration.File;
using System.Security.Cryptography;

namespace Ocelot.AcceptanceTests.Request;

[Trait("PR", "1972")]
public sealed class StreamContentTests : Steps, IDisposable
{
    private readonly ServiceHandler _serviceHandler;

    public StreamContentTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_stream_with_content_length()
    {
        var contentSize = 1024L * 1024L * 1024L; // 1GB
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StreamTestContent(contentSize, false)))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(contentSize + ";;" + contentSize))
            .BDDfy();
    }

    [Fact]
    public void Should_stream_with_chunked_content()
    {
        var contentSize = 1024L * 1024L * 1024L; // 1GB
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethods.Post);
        var configuration = GivenConfiguration(route);

        this.Given(x => x.GivenThereIsAServiceRunningOn(DownstreamUrl(port), "/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StreamTestContent(contentSize, true)))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(";chunked;" + contentSize))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath)
    {
        static void options(KestrelServerOptions o)
        {
            o.Limits.MaxRequestBodySize = long.MaxValue;
        }

        _serviceHandler.GivenThereIsAServiceRunningOnWithKestrelOptions(baseUrl, basePath, options, async context =>
        {
            var request = context.Request;
            var response = context.Response;

            long streamLength = 0;
            int readBytes;
            var buffer = new byte[8192 - 1]; // Not aligned to sender

            do
            {
                readBytes = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                streamLength += readBytes;
            } while (readBytes > 0);

            response.StatusCode = 200;
            await response.WriteAsync(request.ContentLength + ";" + request.Headers.TransferEncoding + ";" + streamLength);
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

internal class StreamTestContent : HttpContent
{
    private readonly long _size;
    private readonly bool _sendChunked;
    private readonly byte[] _dataBuffer;

    public StreamTestContent(long size, bool sendChunked)
    {
        _size = size;
        _sendChunked = sendChunked;
        _dataBuffer = RandomNumberGenerator.GetBytes(8192);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
    {
        var remaining = _size;
        while (remaining > 0)
        {
            var count = (int)Math.Min(remaining, _dataBuffer.Length);
            await stream.WriteAsync(_dataBuffer, 0, count);
            remaining -= count;
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        if (_sendChunked)
        {
            length = -1;
            return false;
        }
        else
        {
            length = _size;
            return true;
        }
    }
}
