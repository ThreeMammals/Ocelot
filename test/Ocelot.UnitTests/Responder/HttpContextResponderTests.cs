using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Ocelot.Headers;
using Ocelot.Middleware;
using Ocelot.Responder;

namespace Ocelot.UnitTests.Responder
{
    public class HttpContextResponderTests
    {
        private readonly HttpContextResponder _responder;

        public HttpContextResponderTests()
        {
            var removeOutputHeaders = new RemoveOutputHeaders();
            _responder = new HttpContextResponder(removeOutputHeaders);
        }

        [Fact]
        public async Task should_remove_transfer_encoding_header()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>
                {
                    new("Transfer-Encoding", new List<string> {"woop"}),
                }, "some reason");

            await _responder.SetResponseOnHttpContext(httpContext, response);
            var header = httpContext.Response.Headers["Transfer-Encoding"];
            header.ShouldBeEmpty();
        }

        [Fact]
        public async Task should_ignore_content_if_null()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(null, HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

            await Should.NotThrowAsync(async () =>
            {
                await _responder.SetResponseOnHttpContext(httpContext, response);
            });
        }

        [Fact]
        public async Task should_have_content_length()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent("test"), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>(), "some reason");

            await _responder.SetResponseOnHttpContext(httpContext, response);
            var header = httpContext.Response.Headers["Content-Length"];
            header.First().ShouldBe("4");
        }

        [Fact]
        public async Task should_add_header()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>
                {
                    new("test", new List<string> {"test"}),
                }, "some reason");

            await _responder.SetResponseOnHttpContext(httpContext, response);
            var header = httpContext.Response.Headers["test"];
            header.First().ShouldBe("test");
        }

        [Fact]
        public async Task should_add_reason_phrase()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent(string.Empty), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>
                {
                    new("test", new List<string> {"test"}),
                }, "some reason");

            await _responder.SetResponseOnHttpContext(httpContext, response);
            httpContext.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase.ShouldBe(response.ReasonPhrase);
        }

        [Fact]
        public void should_call_without_exception()
        {
            var httpContext = new DefaultHttpContext();
            _responder.SetErrorResponseOnContext(httpContext, 500);
        }
    }
}
