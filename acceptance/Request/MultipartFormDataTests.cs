using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Ocelot.Acceptance.Request;

[Trait("Bug", "714")] // https://github.com/ThreeMammals/Ocelot/issues/714
[Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
public sealed class MultipartFormDataTests : Steps
{
    private DownstreamData[] _downstream;
    public struct DownstreamData(string path, string contentType, string fieldValue, string fileFieldName, string fileName, string fileContentType, string fileContent)
    {
        public string Path = path;
        public string ContentType = contentType;
        public string FieldValue = fieldValue;
        public string FileFieldName = fileFieldName;
        public string FileName = fileName;
        public string FileContentType = fileContentType;
        public string FileContent = fileContent;
    }

    [Fact]
    [Trait("PR", "2421")] // https://github.com/ThreeMammals/Ocelot/pull/2421
    public async Task Should_route_multipart_form_data_with_file()
    {
        const string upstreamPath = "/upload";
        const string downstreamPath = "/api/files";
        const string fieldName = "description";
        const string fieldValue = "issue-714";
        const string fileFieldName = "file";
        const string fileName = "test.txt";
        const string fileContentType = "text/plain";
        const string fileContent = "multipart-file-content";

        var port = PortFinder.GetRandomPort();
        _downstream = new DownstreamData[1];
        var route = GivenRoute(port, HttpMethod.Post, upstreamPath, downstreamPath);
        var configuration = GivenConfiguration(route);
        using var content = GivenMultipartContent(fieldName, fieldValue, fileFieldName, fileName, fileContentType, fileContent);

        GivenThereIsAServiceRunningOn(port, downstreamPath,
                context => CaptureMultipartRequest(context, fieldName, fileFieldName, null));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway(upstreamPath, content);
        ThenTheStatusCodeShouldBeOk();
        ThenTheDownstreamRequestShouldMatch(0, downstreamPath, fieldValue, fileFieldName, fileName, fileContentType, fileContent);
    }

    [Fact]
    [Trait("PR", "2421")] // https://github.com/ThreeMammals/Ocelot/pull/2421
    public async Task Should_route_multipart_form_data_with_file_when_single_post_route_is_aggregated()
    {
        const string upstreamPath = "/upload";
        const string routeUpstreamPath = "/upload-route";
        const string downstreamPath = "/api/files";
        const string routeKey = "multipart-upload";
        const string fieldName = "description";
        const string fieldValue = "issue-714";
        const string fileFieldName = "file";
        const string fileName = "test.txt";
        const string fileContentType = "text/plain";
        const string fileContent = "multipart-file-content";

        var port = PortFinder.GetRandomPort();
        _downstream = new DownstreamData[1];
        var route = GivenRoute(port, HttpMethod.Post, routeUpstreamPath, downstreamPath);
        route.Key = routeKey;
        var configuration = GivenAggregatedConfiguration(HttpMethods.Post, upstreamPath, route);
        using var content = GivenMultipartContent(fieldName, fieldValue, fileFieldName, fileName, fileContentType, fileContent);

        GivenThereIsAServiceRunningOn(port, downstreamPath,
                context => CaptureMultipartRequest(context, fieldName, fileFieldName, "UPLOAD-OK"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway(upstreamPath, content);
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyShouldBeAsync("UPLOAD-OK");
        ThenTheDownstreamRequestShouldMatch(0, downstreamPath, fieldValue, fileFieldName, fileName, fileContentType, fileContent);
    }

    private static MultipartFormDataContent GivenMultipartContent(
        string fieldName, string fieldValue, string fileFieldName, string fileName, string fileContentType, string fileContent)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(fieldValue), fieldName }
        };
        var fileBytes = new ByteArrayContent(Encoding.UTF8.GetBytes(fileContent));
        fileBytes.Headers.ContentType = new(fileContentType);
        content.Add(fileBytes, fileFieldName, fileName);

        return content;
    }

    private FileConfiguration GivenAggregatedConfiguration(string method, string upstreamPath, params FileRoute[] routes)
    {
        var configuration = GivenConfiguration(routes);
        configuration.Aggregates.Add(new()
        {
            RouteKeys = [.. routes.Select(r => r.Key)], // [ "Laura", "Tom" ]
            UpstreamHttpMethod = [ method.IfEmpty(HttpMethods.Post) ],
            UpstreamPathTemplate = upstreamPath,
        });
        return configuration;
    }

    private async Task CaptureMultipartRequest(HttpContext context, string fieldName, string fileFieldName, [CallerMemberName] string responseBody = null)
    {
        var request = context.Request;
        var form = await request.ReadFormAsync(context.RequestAborted);
        var file = form.Files.GetFile(fileFieldName).ShouldNotBeNull();
        DownstreamData data = new()
        {
            Path = request.PathBase.Add(request.Path).Value,
            ContentType = request.ContentType,
            FieldValue = form[fieldName].ToString(),
            FileFieldName = file.Name,
            FileName = file.FileName,
            FileContentType = file.ContentType,
        };

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        data.FileContent = await reader.ReadToEndAsync(context.RequestAborted);
        _downstream[0] = data;

        context.Response.StatusCode = StatusCodes.Status200OK;
        if (responseBody is not null)
        {
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(responseBody, context.RequestAborted);
        }
    }

    [Fact]
    [Trait("PR", "2421")] // https://github.com/ThreeMammals/Ocelot/pull/2421
    public async Task Should_multiplex_multipart_form_data_with_file_to_multiple_downstream_routes()
    {
        const string upstreamPath = "/upload";
        const string downstreamPath1 = "/api/files/service1";
        const string downstreamPath2 = "/api/files/service2";
        const string fieldName = "description";
        const string fieldValue = "issue-714-multiplex";
        const string fileFieldName = "file";
        const string fileName = "test.txt";
        const string fileContentType = "text/plain";
        const string fileContent = "multipart-file-content-for-multiplex";

        var port1 = PortFinder.GetRandomPort();
        var port2 = PortFinder.GetRandomPort();
        _downstream = new DownstreamData[2];
        var route1 = GivenRoute(port1, HttpMethod.Post, upstreamPath + 1, downstreamPath1);
        route1.Key = nameof(route1);
        var route2 = GivenRoute(port2, HttpMethod.Post, upstreamPath + 2, downstreamPath2);
        route2.Key = nameof(route2);

        var configuration = GivenAggregatedConfiguration(HttpMethods.Post, upstreamPath, route1, route2);
        using var content = GivenMultipartContent(fieldName, fieldValue, fileFieldName, fileName, fileContentType, fileContent);

        GivenThereIsAServiceRunningOn(port1, downstreamPath1,
                context => CaptureMultipartRequestMultiplex(0, context, fieldName, fileFieldName));
        GivenThereIsAServiceRunningOn(port2, downstreamPath2,
                context => CaptureMultipartRequestMultiplex(1, context, fieldName, fileFieldName));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway(upstreamPath, content);
        ThenTheStatusCodeShouldBeOk();

        _downstream.Length.ShouldBe(2);
        ThenTheDownstreamRequestShouldMatch(0, downstreamPath1, fieldValue, fileFieldName, fileName, fileContentType, fileContent);
        ThenTheDownstreamRequestShouldMatch(1, downstreamPath2, fieldValue, fileFieldName, fileName, fileContentType, fileContent);
        await ThenTheResponseBodyShouldBeAsync($"{{\"route1\":\"{TestName()}-1\",\"route2\":\"{TestName()}-2\"}}");
    }

    private async Task CaptureMultipartRequestMultiplex(int index,
        HttpContext context, string fieldName, string fileFieldName, [CallerMemberName] string responseBody = null)
    {
        var request = context.Request;
        var form = await request.ReadFormAsync(context.RequestAborted);
        var file = form.Files.GetFile(fileFieldName).ShouldNotBeNull();

        var path = request.PathBase.Add(request.Path).Value;
        var fieldValue = form[fieldName].ToString();
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var fileContent = await reader.ReadToEndAsync(context.RequestAborted);

        _downstream[index] = new(path, request.ContentType, fieldValue, file.Name, file.FileName, file.ContentType, fileContent);
        context.Response.StatusCode = StatusCodes.Status200OK;
        if (responseBody is not null)
        {
            context.Response.ContentType = MediaTypeNames.Application.Json;
            var json = $"\"{responseBody}-{++index}\"";
            await context.Response.WriteAsync(json, context.RequestAborted);
        }
    }

    private void ThenTheDownstreamRequestShouldMatch(int i,
        string downstreamPath, string fieldValue, string fileFieldName, string fileName, string fileContentType, string fileContent)
    {
        var data = _downstream[i];
        data.Path.ShouldBe(downstreamPath);
        data.ContentType.ShouldStartWith("multipart/form-data; boundary=");
        data.FieldValue.ShouldBe(fieldValue);
        data.FileFieldName.ShouldBe(fileFieldName);
        data.FileName.ShouldBe(fileName);
        data.FileContentType.ShouldBe(fileContentType);
        data.FileContent.ShouldBe(fileContent);
    }

    [Fact]
    public async Task Should_route_request_to_downstream_service()
    {
        // Arrange: Set up test services and routes
        var downstreamPort = PortFinder.GetRandomPort();
        GivenThereIsAServiceRunningOn(downstreamPort, "/api/endpoint",
            async context =>
            {
                // var json = "{ \"message\": \"success\" }";
                await context.Response.WriteAsJsonAsync(new { message = "success" }, context.RequestAborted);
                // await context.Response.WriteAsync(json, context.RequestAborted);
            });
        var route = GivenRoute(downstreamPort, "/gateway/endpoint", "/api/endpoint");
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        // Act: Send request through the gateway
        await WhenIGetUrlOnTheApiGateway("/gateway/endpoint");

        // Assert: Verify the response
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyShouldBeAsync("{\"message\":\"success\"}");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>(CancelMe);
        content.ValueKind.ShouldBe(JsonValueKind.Object); // sanity check
        content.GetProperty("message").GetString().ShouldBe("success");
    }
}
