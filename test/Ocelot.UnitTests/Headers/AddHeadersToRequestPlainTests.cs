using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Configuration.Creator;
using Ocelot.Headers;
using Ocelot.Infrastructure.Claims.Parser;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Headers
{
    public class AddHeadersToRequestPlainTests
    {
        private readonly AddHeadersToRequest _addHeadersToRequest;
        private HttpContext _context;
        private AddHeader _addedHeader;

        public AddHeadersToRequestPlainTests()
        {
            _addHeadersToRequest = new AddHeadersToRequest(Mock.Of<IClaimsParser>());
        }

        [Fact]
        public void should_add_plain_text_header_to_downstream_request()
        {
            this.Given(_ => GivenHttpRequestWithoutHeaders())
                .When(_ => WhenAddingHeader("X-Custom-Header", "PlainValue"))
                .Then(_ => ThenTheHeaderGetsTakenOverToTheRequestHeaders())
                .BDDfy();
        }

        [Fact]
        public void should_overwrite_existing_header_with_added_header()
        {
            this.Given(_ => GivenHttpRequestWithHeader("X-Custom-Header", "This should get overwritten"))
                .When(_ => WhenAddingHeader("X-Custom-Header", "PlainValue"))
                .Then(_ => ThenTheHeaderGetsTakenOverToTheRequestHeaders())
                .BDDfy();
        }

        private void GivenHttpRequestWithoutHeaders()
        {
            _context = new DefaultHttpContext();
        }

        private void GivenHttpRequestWithHeader(string headerKey, string headerValue)
        {
            _context = new DefaultHttpContext
            {
                Request =
                {
                    Headers =
                    {
                        { headerKey, headerValue }
                    }
                }
            };
        }

        private void WhenAddingHeader(string headerKey, string headerValue)
        {
            _addedHeader = new AddHeader(headerKey, headerValue);
            _addHeadersToRequest.SetHeadersOnDownstreamRequest(new[] { _addedHeader }, _context);
        }
        
        private void ThenTheHeaderGetsTakenOverToTheRequestHeaders()
        {
            var requestHeaders = _context.Request.Headers;
            requestHeaders.ContainsKey(_addedHeader.Key).ShouldBeTrue($"Header {_addedHeader.Key} was expected but not there.");
            var value = requestHeaders[_addedHeader.Key];
            value.ShouldNotBeNull($"Value of header {_addedHeader.Key} was expected to not be null.");
            value.ToString().ShouldBe(_addedHeader.Value);
        }
    }
}
