using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.Net.Mime;

namespace Ocelot.Acceptance.Request;

public sealed class ContentTests : Steps
{
    private string _contentType;
    private long? _contentLength;
    private long _memoryUsageAfterCallToService;
    private bool _contentTypeHeaderExists;

    [Fact]
    public async Task Should_Not_add_content_type_or_content_length_headers()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Get);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.OK, "Hello from Laura");
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("Hello from Laura");
        ThenTheContentTypeShouldBeEmpty();
        ThenTheContentLengthShouldBeZero();
    }

    [Fact]
    public async Task Should_add_content_type_and_content_length_headers()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        var contentType = MediaTypeNames.Application.Json;
        GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Created, string.Empty);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", "postContent", contentType);
        ThenTheStatusCodeShouldBe(HttpStatusCode.Created);
        ThenTheContentTypeIsIs(contentType);
    }

    [Fact]
    public async Task Should_add_default_content_type_header()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        GivenThereIsAServiceRunningOn(port, "/", HttpStatusCode.Created, string.Empty);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway("/", "postContent");
        ThenTheStatusCodeShouldBe(HttpStatusCode.Created);
        ThenTheContentTypeIsIs("text/plain; charset=utf-8");
    }

    [Fact]
    [Trait("PR", "1824")] // https://github.com/ThreeMammals/Ocelot/pull/1824
    [Trait("Feat", "356")] // https://github.com/ThreeMammals/Ocelot/issues/356
    [Trait("Feat", "695")] // https://github.com/ThreeMammals/Ocelot/issues/695
    [Trait("Bug", "1924")] // https://github.com/ThreeMammals/Ocelot/issues/1924
    public async Task Should_Not_increase_memory_usage_When_downloading_large_file()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Get);
        var configuration = GivenConfiguration(route);
        var dummyDatFilePath = GenerateDummyDatFile(100); // large file
        GivenThereIsAServiceWithPayloadRunningOn(port, "/", dummyDatFilePath);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenMemoryUsageShouldNotIncrease();
    }

    private void ThenMemoryUsageShouldNotIncrease()
    {
        var currentMemoryUsage = Process.GetCurrentProcess().WorkingSet64;
        var tolerance = currentMemoryUsage - (10 * 1024 * 1024L);
        Assert.InRange(_memoryUsageAfterCallToService, currentMemoryUsage - tolerance, currentMemoryUsage + tolerance);
    }

    private void ThenTheContentTypeIsIs(string expected)
    {
        _contentType.ShouldBe(expected);
    }

    private void ThenTheContentLengthShouldBeZero()
    {
        _contentLength.ShouldBeNull();
    }

    private void ThenTheContentTypeShouldBeEmpty()
    {
        _contentType.ShouldBeNullOrEmpty();
        _contentTypeHeaderExists.ShouldBe(false);
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, HttpStatusCode statusCode, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, context =>
        {
            _contentType = context.Request.ContentType;
            _contentLength = context.Request.ContentLength;
            _contentTypeHeaderExists = context.Request.Headers.TryGetValue(HeaderNames.ContentType, out var value);
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(responseBody);
        });
    }

    private void GivenThereIsAServiceWithPayloadRunningOn(int port, string basePath, string dummyDatFilePath)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, async context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await using var fileStream = File.OpenRead(dummyDatFilePath);
            await fileStream.CopyToAsync(context.Response.Body);
            _memoryUsageAfterCallToService = Process.GetCurrentProcess().WorkingSet64;
        });
    }

    /// <summary>
    /// Generates a dummy payload of the given size in MB.
    /// Avoiding maintaining a large file in the repository.
    /// </summary>
    /// <param name="sizeInMb">The file size in MB.</param>
    /// <returns>The payload file path.</returns>
    /// <exception cref="ArgumentNullException">Throwing an exception if the payload path is null.</exception>
    private static string GenerateDummyDatFile(int sizeInMb)
    {
        var payloadName = "dummy.dat";
        var payloadPath = Path.Combine(Directory.GetCurrentDirectory(), payloadName);

        if (File.Exists(payloadPath))
        {
            File.Delete(payloadPath);
        }

        var newFile = new FileStream(payloadPath, FileMode.CreateNew);
        try
        {
            newFile.Seek(sizeInMb * 1024L * 1024, SeekOrigin.Begin);
            newFile.WriteByte(0);
        }
        finally
        {
            newFile.Dispose();
        }

        return payloadPath;
    }
}
