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
using Ocelot.Infrastructure;
using Ocelot.UnitTests.Responder;
using System;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Headers
{
    public class AddHeadersToResponseTests
    {
        private IAddHeadersToResponse _adder;
        private Mock<IPlaceholders> _placeholders;
        private HttpResponseMessage _response;
        private List<AddHeader> _addHeaders;
        private Mock<IOcelotLoggerFactory> _factory;
        private Mock<IOcelotLogger> _logger;

        public AddHeadersToResponseTests()
        {
            _factory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _factory.Setup(x => x.CreateLogger<AddHeadersToResponse>()).Returns(_logger.Object);
            _placeholders = new Mock<IPlaceholders>();
            _adder = new AddHeadersToResponse(_placeholders.Object, _factory.Object);
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

        [Fact]
        public void should_do_nothing_and_log_error()
        {
            var addHeaders = new List<AddHeader>
            {
                new AddHeader("Trace-Id", "{TraceId}")
            };
             
            this.Given(_ => GivenAResponseMessage())
                .And(_ => GivenTheTraceIdErrors())
                .And(_ => GivenTheAddHeaders(addHeaders))
                .When(_ => WhenIAdd())
                .Then(_ => ThenTheHeaderIsNotAdded("Trace-Id"))
                .And(_ => ThenTheErrorIsLogged())
                .BDDfy();
        }

        private void ThenTheErrorIsLogged()
        {
            _logger.Verify(x => x.LogError("Unable to add header to response Trace-Id: {TraceId}"), Times.Once);
        }

        private void ThenTheHeaderIsNotAdded(string key)
        {
            _response.Headers.TryGetValues(key, out var values).ShouldBeFalse();
        }

        private void GivenTheTraceIdIs(string traceId)
        {
            _placeholders.Setup(x => x.Get("{TraceId}")).Returns(new OkResponse<string>(traceId));
        }

        private void GivenTheTraceIdErrors()
        {
            _placeholders.Setup(x => x.Get("{TraceId}")).Returns(new ErrorResponse<string>(new AnyError()));
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