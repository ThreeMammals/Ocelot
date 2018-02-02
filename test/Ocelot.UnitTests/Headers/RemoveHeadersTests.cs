using System.Net.Http;
using System.Net.Http.Headers;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Headers
{
    public class RemoveHeadersTests
    {
        private HttpResponseHeaders _headers;
        private readonly Ocelot.Headers.RemoveOutputHeaders _removeOutputHeaders;
        private Response _result;

        public RemoveHeadersTests()
        {
            _removeOutputHeaders = new Ocelot.Headers.RemoveOutputHeaders();
        }

        [Fact]
        public void should_remove_header()
        {
            var httpResponse = new HttpResponseMessage()
            {
                Headers = {{ "Transfer-Encoding", "chunked"}}
            };

            this.Given(x => x.GivenAHttpContext(httpResponse.Headers))
                .When(x => x.WhenIRemoveTheHeaders())
                .Then(x => x.TheHeaderIsNoLongerInTheContext())
                .BDDfy();
        }

        private void GivenAHttpContext(HttpResponseHeaders headers)
        {
            _headers = headers;
        }

        private void WhenIRemoveTheHeaders()
        {
            _result = _removeOutputHeaders.Remove(_headers);
        }

        private void TheHeaderIsNoLongerInTheContext()
        {
            _result.IsError.ShouldBeFalse();
            _headers.ShouldNotContain(x => x.Key == "Transfer-Encoding");
            _headers.ShouldNotContain(x => x.Key == "transfer-encoding");
        }
    }
}
