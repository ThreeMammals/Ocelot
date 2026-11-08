using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net.Mime;
using System.Text;

namespace Ocelot.Acceptance.Request;

public sealed class GzipTests : Steps
{
    [Fact]
    [Trait("Bug", "263")] // https://github.com/ThreeMammals/Ocelot/issues/263
    [Trait("Feat", "267")] // https://github.com/ThreeMammals/Ocelot/pull/267
    public async Task Should_return_response_200_with_simple_url()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port).WithMethods(HttpMethods.Post);
        var configuration = GivenConfiguration(route);
        var input = "people";
        GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura", "\"people\"");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", GivenThePostHasGzipContent(input));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
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
        content.Headers.ContentType = new(MediaTypeNames.Application.Json);
        content.Headers.ContentEncoding.Add("gzip");
        return content;
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody, string expected)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            if (context.Request.Headers.TryGetValue(HeaderNames.ContentEncoding, out var contentEncoding))
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
