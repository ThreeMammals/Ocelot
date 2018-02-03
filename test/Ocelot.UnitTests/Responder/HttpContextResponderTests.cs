using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Ocelot.Headers;
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
            var httpResponseMessage = new HttpResponseMessage {Content = new StringContent("")};
            httpResponseMessage.Headers.Add("Transfer-Encoding", "woop");
            _responder.SetResponseOnHttpContext(httpContext, httpResponseMessage).GetAwaiter().GetResult();
            var header = httpContext.Response.Headers["Transfer-Encoding"];
            header.ShouldBeEmpty();
        }

        [Fact]
        public void should_have_content_length()
        {
            var httpContext = new DefaultHttpContext();
            var httpResponseMessage = new HttpResponseMessage { Content = new StringContent("test") };
            _responder.SetResponseOnHttpContext(httpContext, httpResponseMessage).GetAwaiter().GetResult();
            var header = httpContext.Response.Headers["Content-Length"];
            header.First().ShouldBe("4");
        }

        [Fact]
        public void should_add_header()
        {
            var httpContext = new DefaultHttpContext();
            var httpResponseMessage = new HttpResponseMessage { Content = new StringContent("test") };
            httpResponseMessage.Headers.Add("test", "test");
            _responder.SetResponseOnHttpContext(httpContext, httpResponseMessage).GetAwaiter().GetResult();
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
