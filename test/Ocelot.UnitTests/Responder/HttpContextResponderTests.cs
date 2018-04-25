using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Ocelot.Headers;
using Ocelot.Middleware;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Responder;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Responder
{
    public class HttpContextResponderTests
    {
        private readonly HttpContextResponder _responder;
        private RemoveOutputHeaders _removeOutputHeaders;

        public HttpContextResponderTests()
        {
            _removeOutputHeaders = new RemoveOutputHeaders();
            _responder = new HttpContextResponder(_removeOutputHeaders);
        }

        [Fact]
        public void should_remove_transfer_encoding_header()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent(""), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>
                {
                    new KeyValuePair<string, IEnumerable<string>>("Transfer-Encoding", new List<string> {"woop"})
                });

            _responder.SetResponseOnHttpContext(httpContext, response).GetAwaiter().GetResult();
            var header = httpContext.Response.Headers["Transfer-Encoding"];
            header.ShouldBeEmpty();
        }

        [Fact]
        public void should_have_content_length()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent("test"), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>());

            _responder.SetResponseOnHttpContext(httpContext, response).GetAwaiter().GetResult();
            var header = httpContext.Response.Headers["Content-Length"];
            header.First().ShouldBe("4");
        }

        [Fact]
        public void should_add_header()
        {
            var httpContext = new DefaultHttpContext();
            var response = new DownstreamResponse(new StringContent(""), HttpStatusCode.OK,
                new List<KeyValuePair<string, IEnumerable<string>>>
                {
                    new KeyValuePair<string, IEnumerable<string>>("test", new List<string> {"test"})
                });

            _responder.SetResponseOnHttpContext(httpContext, response).GetAwaiter().GetResult();
            var header = httpContext.Response.Headers["test"];
            header.First().ShouldBe("test");
        }

        [Fact]
        public void should_call_without_exception()
        {
            var httpContext = new DefaultHttpContext();
            _responder.SetErrorResponseOnContext(httpContext, 500);
        }
    }
}
