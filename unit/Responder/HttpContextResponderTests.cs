using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Ocelot.Headers;
using Ocelot.Middleware;
using Ocelot.Responder;

namespace Ocelot.UnitTests.Responder;

public class HttpContextResponderTests : UnitTest
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
                new(HeaderNames.TransferEncoding, ["woop"]),
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
        var header = httpContext.Response.Headers.ContentLength;
        Assert.NotNull(header);
        Assert.True(header.HasValue);
        Assert.Equal(4, header.Value);
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

    [Fact]
    public async Task SetErrorResponseOnContext_HttpContext_DownstreamResponse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        using var ms = new MemoryStream();
        context.Response.Body = ms; // replace Stream.Null !!!

        const string responseBody = "error payload";
        var downstream = new DownstreamResponse(
            new StringContent(responseBody),
            HttpStatusCode.BadRequest,
            new List<KeyValuePair<string, IEnumerable<string>>>(),
            "some reason");

        // Act
        await _responder.SetErrorResponseOnContext(context, downstream);

        // Assert
        Assert.NotNull(context.Response.Headers.ContentLength);
        Assert.Equal(downstream.Content.Headers.ContentLength, context.Response.Headers.ContentLength);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = context.Response.Body.AsString();
        Assert.Equal(responseBody, body);
    }
}
