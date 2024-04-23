using Microsoft.AspNetCore.Http;
using Ocelot.Request.Mapper;
using System.Reflection;
using System.Text;

namespace Ocelot.UnitTests.Request.Mapper;

public class StreamHttpContentTests
{
    private readonly HttpContext _httpContext;

    private const string PayLoad =
        "[{\"_id\":\"65416ef7eafdf7953c4d7319\",\"index\":0,\"guid\":\"254b515d-0569-494d-9bc8-e21c8bd0365e\",\"isActive\":false,\"balance\":\"$1,225.59\",\"picture\":\"http://placehold.it/32x32\",\"age\":26,\"eyeColor\":\"blue\",\"name\":\"FayHatfield\",\"gender\":\"female\",\"company\":\"VIASIA\",\"email\":\"fayhatfield@viasia.com\",\"phone\":\"+1(970)416-2792\",\"address\":\"768MontroseAvenue,Mansfield,NewMexico,8890\",\"about\":\"Duisoccaecatdoloreeiusmoddoipsummollitaliquipnostrudqui.Cillumdoexercitationexercitationexcepteurincididuntadipisicingminimconsecteturofficiaanimdoloreincididuntlaborealiqua.Tempordoloreirurecillumadnullasuntoccaecatsitnulladosit.Sitnostrudullamcolaborisvelitvelitetofficiasitenimipsumaute.\\r\\n\",\"registered\":\"2023-07-03T03:10:08-02:00\",\"latitude\":0.117661,\"longitude\":-65.570177,\"tags\":[\"Lorem\",\"consequat\",\"consectetur\",\"pariatur\",\"fugiat\",\"est\",\"mollit\"],\"friends\":[{\"id\":0,\"name\":\"LynetteMelendez\"},{\"id\":1,\"name\":\"DrakeMay\"},{\"id\":2,\"name\":\"JenningsConrad\"}],\"greeting\":\"Hello,FayHatfield!Youhave3unreadmessages.\",\"favoriteFruit\":\"apple\"}]";

    public StreamHttpContentTests()
    {
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task Copy_body_to_stream_and_stream_content_should_match_payload()
    {
        var sut = StreamHttpContentFactory();
        using var stream = new MemoryStream();
        await sut.CopyToAsync(stream);

        stream.Position = 0;
        var result = Encoding.UTF8.GetString(stream.ToArray());
        result.ShouldBe(PayLoad);
    }

    [Fact]
    public async Task Copy_body_to_stream_with_unknown_length_and_stream_content_should_match_payload()
    {
        var bytes = Encoding.UTF8.GetBytes(PayLoad);
        using var inputStream = new MemoryStream(bytes);
        using var outputStream = new MemoryStream();
        await CopyAsyncTest(
            new StreamHttpContent(_httpContext),
            new object[] { inputStream, outputStream, StreamHttpContent.UnknownLength, false, CancellationToken.None });
        inputStream.Position = 0;
        outputStream.Position = 0;
        var result = Encoding.UTF8.GetString(outputStream.ToArray());
        result.ShouldBe(PayLoad);
    }

    [Fact]
    public async Task Copy_body_to_stream_with_body_length_and_stream_content_should_match_payload()
    {
        var bytes = Encoding.UTF8.GetBytes(PayLoad);
        using var inputStream = new MemoryStream(bytes);
        using var outputStream = new MemoryStream();
        await CopyAsyncTest(
            new StreamHttpContent(_httpContext),
            new object[] { inputStream, outputStream, bytes.Length, false, CancellationToken.None });
        inputStream.Position = 0;
        outputStream.Position = 0;
        var result = Encoding.UTF8.GetString(outputStream.ToArray());
        result.ShouldBe(PayLoad);
    }

    [Fact]
    public async Task Should_throw_if_passed_body_length_does_not_match_real_body_length()
    {
        var bytes = Encoding.UTF8.GetBytes(PayLoad);
        using var inputStream = new MemoryStream(bytes);
        using var outputStream = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CopyAsyncTest(
                new StreamHttpContent(_httpContext),
                new object[] { inputStream, outputStream, 10, false, CancellationToken.None }));
    }

    private StreamHttpContent StreamHttpContentFactory()
    {
        var bytes = Encoding.UTF8.GetBytes(PayLoad);
        _httpContext.Request.Body = new MemoryStream(bytes);
        return new StreamHttpContent(_httpContext);
    }

    private static async Task CopyAsyncTest(StreamHttpContent streamHttpContent, object[] parameters)
    {
        var bindingAttr = BindingFlags.NonPublic | BindingFlags.Static;
        var method = typeof(StreamHttpContent).GetMethod("CopyAsync", bindingAttr) ??
            throw new Exception("Could not find CopyAsync");
        var task = (Task)method.Invoke(streamHttpContent, parameters) ??
                   throw new Exception("Could not invoke CopyAsync");
        await task.ConfigureAwait(false);
    }
}
