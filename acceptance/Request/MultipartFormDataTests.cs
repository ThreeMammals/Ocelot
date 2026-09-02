using Microsoft.AspNetCore.Http;
using System.Text;

namespace Ocelot.Acceptance.Request;

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
    [Trait("Bug", "714")]
    public void Should_reroute_multipart_form_data_with_file()
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

        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, downstreamPath,
                context => CaptureMultipartRequest(context, fieldName, fileFieldName)))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway(upstreamPath, content))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => ThenTheDownstreamRequestShouldMatch(
                downstreamPath,
                fieldValue,
                fileFieldName,
                fileName,
                fileContentType,
                fileContent))
            .BDDfy();
    }

    private static MultipartFormDataContent GivenMultipartContent(
        string fieldName,
        string fieldValue,
        string fileFieldName,
        string fileName,
        string fileContentType,
        string fileContent)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(fieldValue), fieldName);

        var fileBytes = new ByteArrayContent(Encoding.UTF8.GetBytes(fileContent));
        fileBytes.Headers.ContentType = new(fileContentType);
        content.Add(fileBytes, fileFieldName, fileName);

        return content;
    }

    private async Task CaptureMultipartRequest(HttpContext context, string fieldName, string fileFieldName)
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
    }

    private void ThenTheDownstreamRequestShouldMatch(
        string downstreamPath,
        string fieldValue,
        string fileFieldName,
        string fileName,
        string fileContentType,
        string fileContent)
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
