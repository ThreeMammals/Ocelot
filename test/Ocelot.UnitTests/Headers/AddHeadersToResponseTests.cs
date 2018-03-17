using Xunit;
using Shouldly;
using TestStack.BDDfy;
using Ocelot.Headers;
using System.Net.Http;
using System.Collections.Generic;
using Ocelot.Configuration.Creator;
using System.Linq;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Headers
{
    public class AddHeadersToResponseTests
    {
        private IAddHeadersToResponse _adder;
        private Mock<IRequestScopedDataRepository> _repo;
        private HttpResponseMessage _response;
        private List<AddHeader> _addHeaders;

        public AddHeadersToResponseTests()
        {
            _repo = new Mock<IRequestScopedDataRepository>();
            _adder = new AddHeadersToResponse(_repo.Object);
        }

        [Fact]
        public void should_add_header()
        {
            var addHeaders = new List<AddHeader>
            {
                new AddHeader("Laura", "Tom")
            };

            this.Given(_ => GivenAResponseMessage())
                .And(_ => GivenTheAddHeaders(addHeaders))
                .When(_ => WhenIAdd())
                .And(_ => ThenTheHeaderIsReturned("Laura", "Tom"))
                .BDDfy();
        }

        [Fact]
        public void should_add_trace_id_placeholder()
        {
            var addHeaders = new List<AddHeader>
            {
                new AddHeader("Trace-Id", "{TraceId}")
            };
             
             var traceId = "123";
            
            this.Given(_ => GivenAResponseMessage())
                .And(_ => GivenTheTraceIdIs(traceId))
                .And(_ => GivenTheAddHeaders(addHeaders))
                .When(_ => WhenIAdd())
                .Then(_ => ThenTheHeaderIsReturned("Trace-Id", traceId))
                .BDDfy();
        }

        [Fact]
        public void should_add_trace_id_placeholder_and_normal()
        {
            var addHeaders = new List<AddHeader>
            {
                new AddHeader("Trace-Id", "{TraceId}"),
                new AddHeader("Tom", "Laura")
            };

            var traceId = "123";

            this.Given(_ => GivenAResponseMessage())
                .And(_ => GivenTheTraceIdIs(traceId))
                .And(_ => GivenTheAddHeaders(addHeaders))
                .When(_ => WhenIAdd())
                .Then(_ => ThenTheHeaderIsReturned("Trace-Id", traceId))
                .Then(_ => ThenTheHeaderIsReturned("Tom", "Laura"))
                .BDDfy();
        }

        private void GivenTheTraceIdIs(string traceId)
        {
            _repo.Setup(x => x.Get<string>("TraceId")).Returns(new OkResponse<string>(traceId));
        }

        private void ThenTheHeaderIsReturned(string key, string value)
        {
            var values = _response.Headers.GetValues(key);
            values.First().ShouldBe(value);
        }

        private void WhenIAdd()
        {
            _adder.Add(_addHeaders, _response);
        }

        private void GivenAResponseMessage()
        {
            _response = new HttpResponseMessage();
        }

        private void GivenTheAddHeaders(List<AddHeader> addHeaders)
        {
            _addHeaders = addHeaders;
        }
    }
}