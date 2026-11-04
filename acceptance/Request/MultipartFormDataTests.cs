using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using System.Net.Mime;
using System.Text;

namespace Ocelot.Acceptance.Request;

[Trait("Bug", "714")] // https://github.com/ThreeMammals/Ocelot/issues/714
[Trait("Release", "25.1.0")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.1.0
public sealed class MultipartFormDataTests : Steps
{
    private string _downstreamPath;
    private string _downstreamContentType;
    private string _downstreamFieldValue;
    private string _downstreamFileFieldName;
    private string _downstreamFileName;
    private string _downstreamFileContentType;
    private string _downstreamFileContent;

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
        var route = GivenRoute(port, HttpMethod.Post, upstreamPath, downstreamPath);
        var configuration = GivenConfiguration(route);
        using var content = GivenMultipartContent(fieldName, fieldValue, fileFieldName, fileName, fileContentType, fileContent);

        GivenThereIsAServiceRunningOn(port, downstreamPath,
                context => CaptureMultipartRequest(context, fieldName, fileFieldName));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway(upstreamPath, content);
        ThenTheStatusCodeShouldBeOk();
        ThenTheDownstreamRequestShouldMatch(downstreamPath, fieldValue, fileFieldName, fileName, fileContentType, fileContent);
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
        var route = GivenRoute(port, HttpMethod.Post, routeUpstreamPath, downstreamPath);
        route.Key = routeKey;
        var configuration = GivenAggregatedConfiguration(route, upstreamPath);
        using var content = GivenMultipartContent(fieldName, fieldValue, fileFieldName, fileName, fileContentType, fileContent);

        GivenThereIsAServiceRunningOn(port, downstreamPath,
                context => CaptureMultipartRequest(context, fieldName, fileFieldName, "UPLOAD-OK"));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway(upstreamPath, content);
        ThenTheStatusCodeShouldBeOk();
        await ThenTheResponseBodyShouldBeAsync("UPLOAD-OK");
        ThenTheDownstreamRequestShouldMatch(downstreamPath, fieldValue, fileFieldName, fileName, fileContentType, fileContent);
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

    private FileConfiguration GivenAggregatedConfiguration(FileRoute route, string upstreamPath)
    {
        var configuration = GivenConfiguration(route);
        configuration.Aggregates.Add(new()
        {
            RouteKeys = [route.Key],
            UpstreamHttpMethod = [HttpMethods.Post],
            UpstreamPathTemplate = upstreamPath,
        });
        return configuration;
    }

    private async Task CaptureMultipartRequest(HttpContext context, string fieldName, string fileFieldName, string responseBody = null)
    {
        var request = context.Request;
        var form = await request.ReadFormAsync(context.RequestAborted);
        var file = form.Files.GetFile(fileFieldName).ShouldNotBeNull();

        _downstreamPath = request.PathBase.Add(request.Path).Value;
        _downstreamContentType = request.ContentType;
        _downstreamFieldValue = form[fieldName].ToString();
        _downstreamFileFieldName = file.Name;
        _downstreamFileName = file.FileName;
        _downstreamFileContentType = file.ContentType;

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        _downstreamFileContent = await reader.ReadToEndAsync(context.RequestAborted);

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
        var route1 = GivenRoute(port1, HttpMethod.Post, upstreamPath, downstreamPath1);
        var route2 = GivenRoute(port2, HttpMethod.Post, upstreamPath, downstreamPath2);
        var configuration = GivenConfiguration(route1, route2);
        using var content = GivenMultipartContent(fieldName, fieldValue, fileFieldName, fileName, fileContentType, fileContent);

        var capturedRequests = new List<(string Path, string FieldValue, string FileFieldName, string FileName, string FileContentType, string FileContent)>();

        GivenThereIsAServiceRunningOn(port1, downstreamPath1,
                context => CaptureMultipartRequestMultiplex(context, fieldName, fileFieldName, capturedRequests, 0));
        GivenThereIsAServiceRunningOn(port2, downstreamPath2,
                context => CaptureMultipartRequestMultiplex(context, fieldName, fileFieldName, capturedRequests, 1));
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        await WhenIPostUrlOnTheApiGateway(upstreamPath, content);
        ThenTheStatusCodeShouldBeOk();

        // Verify that both downstream services received the multipart request with identical data
        capturedRequests.Count.ShouldBe(2);

        // Verify request 1
        capturedRequests[0].Path.ShouldBe(downstreamPath1);
        capturedRequests[0].FieldValue.ShouldBe(fieldValue);
        capturedRequests[0].FileFieldName.ShouldBe(fileFieldName);
        capturedRequests[0].FileName.ShouldBe(fileName);
        capturedRequests[0].FileContentType.ShouldBe(fileContentType);
        capturedRequests[0].FileContent.ShouldBe(fileContent);

        // Verify request 2
        capturedRequests[1].Path.ShouldBe(downstreamPath2);
        capturedRequests[1].FieldValue.ShouldBe(fieldValue);
        capturedRequests[1].FileFieldName.ShouldBe(fileFieldName);
        capturedRequests[1].FileName.ShouldBe(fileName);
        capturedRequests[1].FileContentType.ShouldBe(fileContentType);
        capturedRequests[1].FileContent.ShouldBe(fileContent);
    }

    private static async Task CaptureMultipartRequestMultiplex(
        HttpContext context, string fieldName, string fileFieldName,
        List<(string Path, string FieldValue, string FileFieldName, string FileName, string FileContentType, string FileContent)> capturedRequests,
        int index)
    {
        var request = context.Request;
        var form = await request.ReadFormAsync(context.RequestAborted);
        var file = form.Files.GetFile(fileFieldName).ShouldNotBeNull();

        var path = request.PathBase.Add(request.Path).Value;
        var fieldValue = form[fieldName].ToString();
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var fileContent = await reader.ReadToEndAsync(context.RequestAborted);

        capturedRequests.Insert(index, (path, fieldValue, file.Name, file.FileName, file.ContentType, fileContent));

        context.Response.StatusCode = StatusCodes.Status200OK;
    }

    private void ThenTheDownstreamRequestShouldMatch(
        string downstreamPath, string fieldValue, string fileFieldName, string fileName, string fileContentType, string fileContent)
    {
        _downstreamPath.ShouldBe(downstreamPath);
        _downstreamContentType.ShouldStartWith("multipart/form-data; boundary=");
        _downstreamFieldValue.ShouldBe(fieldValue);
        _downstreamFileFieldName.ShouldBe(fileFieldName);
        _downstreamFileName.ShouldBe(fileName);
        _downstreamFileContentType.ShouldBe(fileContentType);
        _downstreamFileContent.ShouldBe(fileContent);
    }
}
