using System;
using System.Net.Http;
using Ocelot.Request.Middleware;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Request
{
    public class DownstreamRequestTests
    {
        [Fact]
        public void should_handle_query_string()
        {
            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.RequestUri = new Uri("https://example.com/a?b=c");
            var downstreamRequest = new DownstreamRequest(httpRequestMessage);
            var result = downstreamRequest.ToHttpRequestMessage();
            result.RequestUri.Query.ShouldBe("?b=c");
        }
    }
}
