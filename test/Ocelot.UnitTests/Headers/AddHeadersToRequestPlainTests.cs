using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration.Creator;
using Ocelot.Headers;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Claims.Parser;
using Ocelot.Logging;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Headers;

public class AddHeadersToRequestPlainTests : UnitTest
{
    private readonly AddHeadersToRequest _addHeadersToRequest;
    private readonly DefaultHttpContext _context;
    private AddHeader _addedHeader;
    private readonly Mock<IPlaceholders> _placeholders;
    private readonly Mock<IOcelotLoggerFactory> _factory;
    private readonly Mock<IOcelotLogger> _logger;

    public AddHeadersToRequestPlainTests()
    {
        _placeholders = new Mock<IPlaceholders>();
        _factory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _factory.Setup(x => x.CreateLogger<AddHeadersToRequest>()).Returns(_logger.Object);
        _addHeadersToRequest = new AddHeadersToRequest(Mock.Of<IClaimsParser>(), _placeholders.Object, _factory.Object);
        _context = new DefaultHttpContext();
    }

    [Fact]
    [Trait("Feat", "623")] // https://github.com/ThreeMammals/Ocelot/issues/623
    [Trait("PR", "632")] // https://github.com/ThreeMammals/Ocelot/pull/632
    public void Should_log_error_if_cannot_find_placeholder()
    {
        // Arrange
        _placeholders.Setup(x => x.Get(It.IsAny<string>())).Returns(new ErrorResponse<string>(new AnyError()));

        // Act
        WhenAddingHeader("X-Forwarded-For", "{RemoteIdAddress}");

        // Assert
        _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == $"Unable to add header to response X-Forwarded-For: {{RemoteIdAddress}}")), Times.Once);
    }

    [Fact]
    [Trait("Feat", "623")]
    [Trait("PR", "632")]
    public void Should_add_placeholder_to_downstream_request()
    {
        // Arrange
        _placeholders.Setup(x => x.Get(It.IsAny<string>())).Returns(new OkResponse<string>("replaced"));

        // Act
        WhenAddingHeader("X-Forwarded-For", "{RemoteIdAddress}");

        // Assert
        ThenTheHeaderGetsTakenOverToTheRequestHeaders("replaced");
    }

    [Fact]
    public void Should_add_plain_text_header_to_downstream_request()
    {
        // Arrange, Act
        WhenAddingHeader("X-Custom-Header", "PlainValue");

        // Assert
        ThenTheHeaderGetsTakenOverToTheRequestHeaders();
    }

    [Fact]
    public void Should_overwrite_existing_header_with_added_header()
    {
        // Arrange
        _context.Request.Headers.Append("X-Custom-Header", "This should get overwritten");

        // Act
        WhenAddingHeader("X-Custom-Header", "PlainValue");

        // Assert
        ThenTheHeaderGetsTakenOverToTheRequestHeaders();
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
        value.ShouldNotBe(default, $"Value of header {_addedHeader.Key} was expected to not be null.");
        value.ToString().ShouldBe(_addedHeader.Value);
    }

    private void ThenTheHeaderGetsTakenOverToTheRequestHeaders(string expected)
    {
        var requestHeaders = _context.Request.Headers;
        var value = requestHeaders[_addedHeader.Key];
        value.ToString().ShouldBe(expected);
    }
}
