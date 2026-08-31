using Microsoft.AspNetCore.Http;
using System.Text;

namespace Ocelot.Acceptance.Request;

public sealed class MultipartFormDataTests : Steps
{
    private const string FieldName = "description";
    private const string FieldValue = "issue-714";
    private const string FileFieldName = "file";
    private const string FileName = "test.txt";
    private const string FileContent = "multipart-file-content";

    private string _downstreamContentType;
    private string _downstreamFieldValue;
    private string _downstreamFileFieldName;
    private string _downstreamFileName;
    private string _downstreamFileContent;

    [Fact]
    [Trait("Bug", "714")]
    public void Should_route_multipart_form_data_with_file()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, HttpMethod.Post);
        var configuration = GivenConfiguration(route);
        using var content = GivenMultipartContent();

        this
            .Given(x => x.GivenThereIsAServiceRunningOn(port, "/", CaptureMultipartRequest))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIPostUrlOnTheApiGateway("/", content))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(_ => _downstreamContentType.ShouldStartWith("multipart/form-data; boundary="))
            .And(_ => _downstreamFieldValue.ShouldBe(FieldValue))
            .And(_ => _downstreamFileFieldName.ShouldBe(FileFieldName))
            .And(_ => _downstreamFileName.ShouldBe(FileName))
            .And(_ => _downstreamFileContent.ShouldBe(FileContent))
            .BDDfy();
    }

    private static MultipartFormDataContent GivenMultipartContent()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(FieldValue), FieldName);

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(FileContent));
        fileContent.Headers.ContentType = new("text/plain");
        content.Add(fileContent, FileFieldName, FileName);

        return content;
    }

    private async Task CaptureMultipartRequest(HttpContext context)
    {
        var request = context.Request;
        var form = await request.ReadFormAsync(context.RequestAborted);
        var file = form.Files.GetFile(FileFieldName).ShouldNotBeNull();

        _downstreamContentType = request.ContentType;
        _downstreamFieldValue = form[FieldName].ToString();
        _downstreamFileFieldName = file.Name;
        _downstreamFileName = file.FileName;

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        _downstreamFileContent = await reader.ReadToEndAsync(context.RequestAborted);

        context.Response.StatusCode = StatusCodes.Status200OK;
    }
}
