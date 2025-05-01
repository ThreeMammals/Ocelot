using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;

namespace Ocelot.AcceptanceTests;

public sealed class GzipTests : Steps
{
    private readonly ServiceHandler _serviceHandler;

    public GzipTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Should_return_response_200_with_simple_url()
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

        var input = "people";

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}", "/", 200, "Hello from Laura", "\"people\""))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", GivenThePostHasGzipContent(input)))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    private static HttpContent GivenThePostHasGzipContent(object input)
    {
        var json = JsonConvert.SerializeObject(input);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
        {
            gzip.Write(jsonBytes, 0, jsonBytes.Length);
        }

        ms.Position = 0;
        var content = new StreamContent(ms);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.ContentEncoding.Add("gzip");
        return content;
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl, string basePath, int statusCode, string responseBody, string expected)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, async context =>
        {
            if (context.Request.Headers.TryGetValue("Content-Encoding", out var contentEncoding))
            {
                contentEncoding.First().ShouldBe("gzip");

                string text = null;
                using (var decompress = new GZipStream(context.Request.Body, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(decompress))
                    {
                        // Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.
                        // text = sr.ReadToEnd();
                        text = await sr.ReadToEndAsync();
                    }
                }

                if (text != expected)
                {
                    throw new Exception("not gzipped");
                }

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync(responseBody);
            }
            else
            {
                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsync("downstream path didnt match base path");
            }
        });
    }
}
