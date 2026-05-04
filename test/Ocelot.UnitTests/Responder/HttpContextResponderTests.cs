using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using Ocelot.Headers;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responder;

namespace Ocelot.UnitTests.Responder;

public class HttpContextResponderTests
{
    private readonly HttpContextResponder _responder;
    private readonly Mock<IOcelotLogger> _logger;

    public HttpContextResponderTests()
    {
        var removeOutputHeaders = new RemoveOutputHeaders();
        var loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        loggerFactory.Setup(x => x.CreateLogger<HttpContextResponder>()).Returns(_logger.Object);
        _responder = new HttpContextResponder(removeOutputHeaders, loggerFactory.Object);
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

    [Fact]
    [Trait("Feat", "941")]
    public async Task Should_disable_buffering_for_sse_content_type()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var bodyFeature = new MockHttpResponseBodyFeature();
        httpContext.Features.Set<IHttpResponseBodyFeature>(bodyFeature);

        var content = new StringContent("data: test");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
        var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        bodyFeature.DisableBufferingCalled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task Should_disable_buffering_and_add_nginx_header_for_sse_with_charset()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var bodyFeature = new MockHttpResponseBodyFeature();
        httpContext.Features.Set<IHttpResponseBodyFeature>(bodyFeature);

        var content = new StringContent("data: test");
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/event-stream; charset=utf-8");
        var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        bodyFeature.DisableBufferingCalled.ShouldBeTrue();
        httpContext.Response.Headers["X-Accel-Buffering"].ToString().ShouldBe("no");
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task Should_NOT_disable_buffering_for_non_sse_content_type()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var bodyFeature = new MockHttpResponseBodyFeature();
        httpContext.Features.Set<IHttpResponseBodyFeature>(bodyFeature);

        var content = new StringContent("not sse");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        bodyFeature.DisableBufferingCalled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Feat", "941")]
    public async Task Should_log_warning_when_body_feature_is_null_for_sse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Features.Set<IHttpResponseBodyFeature>(null);
        
        var content = new StringContent("data: test");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
        var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Act
        try
        {
            await _responder.SetResponseOnHttpContext(httpContext, response);
        }
        catch (NullReferenceException)
        {
            // Expected because context.Response.Body access fails when feature is null
        }

        // Assert
        _logger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("IHttpResponseBodyFeature is null"))), Times.Once);
    }

    [Theory]
    [Trait("Feat", "941")]
    [InlineData("text/event-stream")]
    [InlineData("text/event-stream; charset=utf-8")]
    [InlineData("TEXT/EVENT-STREAM")]
    public async Task Should_detect_sse_for_various_content_types(string contentType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var bodyFeature = new MockHttpResponseBodyFeature();
        httpContext.Features.Set<IHttpResponseBodyFeature>(bodyFeature);

        var content = new StringContent("data: test");
        content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        var response = new DownstreamResponse(content, HttpStatusCode.OK, new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

        // Act
        await _responder.SetResponseOnHttpContext(httpContext, response);

        // Assert
        bodyFeature.DisableBufferingCalled.ShouldBeTrue();
    }

    private class MockHttpResponseBodyFeature : IHttpResponseBodyFeature
    {
        public bool DisableBufferingCalled { get; private set; }
        public Stream Stream { get; } = new MemoryStream();
        public System.IO.Pipelines.PipeWriter Writer => throw new NotImplementedException();
        public void DisableBuffering() => DisableBufferingCalled = true;
        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task CompleteAsync() => throw new NotImplementedException();
    }
}
