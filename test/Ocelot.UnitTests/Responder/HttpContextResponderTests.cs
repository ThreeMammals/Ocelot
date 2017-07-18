using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Headers;
using Ocelot.Responder;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using TestStack.BDDfy;
using Xunit;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Ocelot.UnitTests.Responder
{
    public class HttpContextResponderTests
    {
        readonly HttpContextResponder _responder;

        readonly Mock<IRemoveOutputHeaders> _removeOutputHeaders;

        HttpContext _httpContext;

        HttpResponseMessage _httpResponseMessage;

        public HttpContextResponderTests()
        {
            _removeOutputHeaders = new Mock<IRemoveOutputHeaders>();
            _httpContext = new DefaultHttpContext();
            _httpResponseMessage = new HttpResponseMessage();
            _httpResponseMessage.Content = new MyHttpContent();
            _responder = new HttpContextResponder(_removeOutputHeaders.Object);
        }

        [Fact]
        public void DoSomething()
        {
            this.Given(_ => GivenTheHttpResponseMessageHasHeader("abc","123"))
                .And(_ => GivenTheHttpResponseMessageHasHeader("def", new[] { "456", "789" }))
                .And(_ => GivenTheContextResponseHasHeader("abc", "123"))
                .When(_ => WhenWeSetTheResponseOnAnHttpContext())
                .Then(_ => ThenSupportedHeadersAreAddedToTheContextResponse())
                .And(_ => ThenUnsupportedHeadersAreNotAddedToTheResponse())
                .BDDfy();
        }


        private void GivenTheHttpResponseMessageHasHeader(string name, string value)
        {
            _httpResponseMessage.Headers.Add(name, value);
        }

        private void GivenTheHttpResponseMessageHasHeader(string name, IEnumerable<string> values)
        {
            _httpResponseMessage.Headers.Add(name, values);
        }

        private void GivenTheContextResponseHasHeader(string name, string value)
        {
            _httpContext.Response.Headers.Add(name, value);
        }

        private void WhenWeSetTheResponseOnAnHttpContext()
        {
            _responder.SetResponseOnHttpContext(_httpContext, _httpResponseMessage).GetAwaiter().GetResult();
        }

        private void ThenSupportedHeadersAreAddedToTheContextResponse()
        {
            _httpContext.Response.Headers.Count.ShouldBe(2);
            _httpContext.Response.Headers.ShouldContain(h => h.Key == "abc" && h.Value == "123");
            _httpContext.Response.Headers.ShouldContain(h => h.Key == "def" && h.Value == "456, 789");
        }

        private void ThenUnsupportedHeadersAreNotAddedToTheResponse()
        {
            _removeOutputHeaders.Verify(roh => roh.Remove(_httpResponseMessage.Headers), Times.Once);
        }


        class MyHttpContent : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return Task.CompletedTask;
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return true;
            }
        }

    }
}
