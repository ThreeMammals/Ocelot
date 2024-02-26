using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;

namespace Ocelot.AcceptanceTests;

public class StreamContentTest : IDisposable
{
    private readonly ServiceHandler _serviceHandler;
    private readonly Steps _steps;

    public class StreamTestContent(long size, bool sendChunked) : HttpContent
    {
        private readonly byte[] _dataBuffer = RandomNumberGenerator.GetBytes(8192);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var remaining = size;
            while (remaining > 0)
            {
                var count = (int)Math.Min(remaining, _dataBuffer.Length);
                await stream.WriteAsync(_dataBuffer, 0, count);
                remaining -= count;
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            if (sendChunked)
            {
                length = -1;
                return false;
            }
            else
            {
                length = size;
                return true;
            }
        }
    }

    public StreamContentTest()
    {
        _serviceHandler = new ServiceHandler();
        _steps = new Steps();
    }

    [Fact]
    public void should_stream_with_content_length()
    {
        var port = PortFinder.GetRandomPort();
        var contentSize = 1024L * 1024L * 1024L; // 1GB

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

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StreamTestContent(contentSize, false)))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(contentSize + ";;" + contentSize))
            .BDDfy();
    }

    [Fact]
    public void should_stream_with_chunked_content()
    {
        var port = PortFinder.GetRandomPort();
        var contentSize = 1024L * 1024L * 1024L; // 1GB

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

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIPostUrlOnTheApiGateway("/", new StreamTestContent(contentSize, true)))
            .Then(x => _steps.ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => _steps.ThenTheResponseBodyShouldBe(";chunked;" + contentSize))
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, long.MaxValue, async context =>
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

    public void Dispose()
    {
        _serviceHandler?.Dispose();
        _steps?.Dispose();
    }
}
