using Ocelot.Configuration.Creator;
using Ocelot.Headers;
using Ocelot.Infrastructure;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Headers;

public class AddHeadersToResponseTests : UnitTest
{
    private readonly AddHeadersToResponse _adder;
    private readonly Mock<IPlaceholders> _placeholders;
    private readonly DownstreamResponse _response;
    private List<AddHeader> _addHeaders;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;

    public AddHeadersToResponseTests()
    {
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _factory.Setup(x => x.CreateLogger<AddHeadersToResponse>()).Returns(_logger.Object);
        _placeholders = new Mock<IPlaceholders>();
        _adder = new AddHeadersToResponse(_placeholders.Object, _factory.Object);
        _response = new DownstreamResponse(new HttpResponseMessage());
    }

    [Fact]
    public void Should_add_header()
    {
        // Arrange
        _addHeaders = new List<AddHeader>
        {
            new("Laura", "Tom"),
        };

        // Act
        _adder.Add(_addHeaders, _response);

        // Assert
        ThenTheHeaderIsReturned("Laura", "Tom");
    }

    [Fact]
    public void Should_add_trace_id_placeholder()
    {
        // Arrange
        _addHeaders = new List<AddHeader>
        {
            new("Trace-Id", "{TraceId}"),
        };
        var traceId = "123";
        GivenTheTraceIdIs(traceId);

        // Act
        _adder.Add(_addHeaders, _response);

        // Assert
        ThenTheHeaderIsReturned("Trace-Id", traceId);
    }

    [Fact]
    public void Should_add_trace_id_placeholder_and_normal()
    {
        // Arrange
        _addHeaders = new List<AddHeader>
        {
            new("Trace-Id", "{TraceId}"),
            new("Tom", "Laura"),
        };
        var traceId = "123";
        GivenTheTraceIdIs(traceId);

        // Act
        _adder.Add(_addHeaders, _response);

        // Assert
        ThenTheHeaderIsReturned("Trace-Id", traceId);
        ThenTheHeaderIsReturned("Tom", "Laura");     
    }

    [Fact]
    public void Should_do_nothing_and_log_error()
    {
        // Arrange
        _addHeaders = new List<AddHeader>
        {
            new("Trace-Id", "{TraceId}"),
        };
        _placeholders.Setup(x => x.Get("{TraceId}")).Returns(new ErrorResponse<string>(new AnyError()));

        // Act
        _adder.Add(_addHeaders, _response);

        // Assert
        _response.Headers.Any(x => x.Key == "Trace-Id").ShouldBeFalse();
        _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == "Unable to add header to response Trace-Id: {TraceId}")), Times.Once);
    }

    private void GivenTheTraceIdIs(string traceId)
    {
        _placeholders.Setup(x => x.Get("{TraceId}")).Returns(new OkResponse<string>(traceId));
    }

    private void ThenTheHeaderIsReturned(string key, string value)
    {
        var values = _response.Headers.First(x => x.Key == key);
        values.Values.First().ShouldBe(value);
    }
}
