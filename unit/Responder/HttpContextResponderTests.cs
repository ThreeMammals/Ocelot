using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Ocelot.Headers;
using Ocelot.Middleware;
using Ocelot.Responder;

namespace Ocelot.UnitTests.Responder;

public class HttpContextResponderTests
{
    private readonly HttpContextResponder _responder;

    public HttpContextResponderTests()
    {
        var removeOutputHeaders = new RemoveOutputHeaders();
        _responder = new HttpContextResponder(removeOutputHeaders);
    }

    [Fact]
    public async Task Should_remove_transfer_encoding_header()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.OK,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("Transfer-Encoding", new List<string> {"woop"}),
            }, "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        var header = httpContext.Response.Headers.TransferEncoding;
        header.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_ignore_content_if_null()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var response = new DownstreamResponse(null, HttpStatusCode.OK,
            new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Assert
        await Should.NotThrowAsync(async () =>
        {
            // Act
            await _responder.SetResponseOnHttpContext(httpContext, response);
        });
    }

    [Fact]
    public async Task Should_have_content_length()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var response = new DownstreamResponse(new StringContent("test"), HttpStatusCode.OK,
            new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        var header = httpContext.Response.Headers["Content-Length"];
        header.First().ShouldBe("4");
    }

    [Fact]
    public async Task Should_add_header()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.OK,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("test", new List<string> {"test"}),
            }, "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        var header = httpContext.Response.Headers["test"];
        header.First().ShouldBe("test");
    }

    [Fact]
    public async Task Should_add_reason_phrase()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.OK,
            new List<KeyValuePair<string, IEnumerable<string>>>
            {
                new("test", new List<string> {"test"}),
            }, "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        httpContext.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase.ShouldBe(response.ReasonPhrase);
    }

    [Fact]
    public void Should_call_without_exception()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act, Assert
        _responder.SetErrorResponseOnContext(httpContext, 500);
    }
}
