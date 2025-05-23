using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;

namespace Ocelot.AcceptanceTests;

public sealed class GzipTests : Steps
{
    public GzipTests()
    {
    }

    [Fact]
    public void Should_return_response_200_with_simple_url()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithMethods(HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var input = "people";
        this.Given(x => x.GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura", "\"people\""))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", GivenThePostHasGzipContent(input)))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe("Hello from Laura"))
            .BDDfy();
    }

    private static StreamContent GivenThePostHasGzipContent(object input)
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

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody, string expected)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            if (context.Request.Headers.TryGetValue("Content-Encoding", out var contentEncoding))
            {
                contentEncoding.First().ShouldBe("gzip");

                string text = null;
                using (var decompress = new GZipStream(context.Request.Body, CompressionMode.Decompress))
                {
                    using var sr = new StreamReader(decompress);
                    text = await sr.ReadToEndAsync();
                }
                if (text != expected)
                {
                    throw new Exception("not gzipped");
                }

                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsync(responseBody);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("downstream path didnt match base path");
            }
        });
    }
}
