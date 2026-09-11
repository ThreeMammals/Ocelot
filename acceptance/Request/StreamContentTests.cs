using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Cryptography;

namespace Ocelot.Acceptance.Request;

[Trait("TODO", "Update me!")]
public sealed class StreamContentTests : Steps
{
#if NET10_0_OR_GREATER
    // TODO Require fixing for net10.0 TFM or streaming feature review.
    [Fact(DisplayName = "TODO " + nameof(Should_stream_with_content_length))]
#else
    [Fact]
#endif
    [Trait("PR", "1972")] // https://github.com/ThreeMammals/Ocelot/pull/1972
    public void Should_stream_with_content_length()
    {
#if NET10_0_OR_GREATER
        Xunit.TestContext.Current.AddWarning(".NET 10 SDK can't handle 1GB — seems like a web server setting is limiting it.");
        var contentSize = 1024L * 1024L; // * 1024L; // 1GB
#else
        var contentSize = 1024L * 1024L * 1024L; // 1GB
#endif
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        using var content = new StreamTestContent(contentSize, false);
        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", content))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(contentSize + ";;" + contentSize))
        .BDDfy();
    }


#if NET10_0_OR_GREATER
    // TODO Require fixing for net10.0 TFM or streaming feature review.
    [Fact(DisplayName = "TODO " + nameof(Should_stream_with_chunked_content))]
#else
    [Fact]
#endif
    [Trait("Feat", "928")] // https://github.com/ThreeMammals/Ocelot/issues/928
    [Trait("PR", "1972")] // https://github.com/ThreeMammals/Ocelot/pull/1972
    public async Task Should_stream_with_chunked_content()
    {
#if NET10_0_OR_GREATER
        Xunit.TestContext.Current.AddWarning(".NET 10 SDK can't handle 1GB — seems like a web server setting is limiting it.");
        var contentSize = 1024L * 1024L; // * 1024L; // 1GB
#else
        var contentSize = 1024L * 1024L * 1024L; // 1GB
#endif
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        /*this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/"))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunningAsync())
            .When(x => WhenIPostUrlOnTheApiGateway("/", new StreamTestContent(contentSize, true)))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(";chunked;" + contentSize))
        .BDDfy();*/
        GivenThereIsAServiceRunningOn(port, "/");
        GivenThereIsAConfiguration(configuration);
        await GivenOcelotIsRunningAsync();
        using var content = new StreamTestContent(contentSize, true);
        await WhenIPostUrlOnTheApiGateway("/", content);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe(";chunked;" + contentSize);
    }

    public override void GivenThereIsAServiceRunningOn(int port, string basePath)
    {
        static void options(KestrelServerOptions o)
        {
            o.Limits.MaxRequestBodySize = long.MaxValue;
        }
        var baseUrl = DownstreamUrl(port);
        handler.GivenThereIsAServiceRunningOnWithKestrelOptions(baseUrl, basePath, options, async context =>
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
