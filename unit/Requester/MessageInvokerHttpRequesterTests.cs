using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester;
using Ocelot.Responses;

namespace Ocelot.UnitTests.Requester;

public class MessageInvokerHttpRequesterTests
{
    private readonly Mock<IOcelotLoggerFactory> _loggerFactoryMock;
    private readonly Mock<IOcelotLogger> _loggerMock;
    private readonly Mock<IMessageInvokerPool> _messageInvokerPoolMock;
    private readonly Mock<IExceptionToErrorMapper> _mapperMock;
    private readonly Mock<HttpMessageInvoker> _messageInvokerMock;
    private readonly MessageInvokerHttpRequester _sut;

    public MessageInvokerHttpRequesterTests()
    {
        _loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        _loggerMock = new Mock<IOcelotLogger>();
        _loggerFactoryMock.Setup(f => f.CreateLogger<MessageInvokerHttpRequester>())
            .Returns(_loggerMock.Object);

        _messageInvokerPoolMock = new Mock<IMessageInvokerPool>();
        _mapperMock = new Mock<IExceptionToErrorMapper>();
        _messageInvokerMock = new Mock<HttpMessageInvoker>(new HttpClientHandler());

        _sut = new MessageInvokerHttpRequester(
            _loggerFactoryMock.Object,
            _messageInvokerPoolMock.Object,
            _mapperMock.Object);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        // Ocelot adds DownstreamRequest and DownstreamRoute into Items
        var downstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test"));
        var downstreamRoute = new DownstreamRouteBuilder().Build();

        context.Items.UpsertDownstreamRequest(downstreamRequest);
        context.Items.UpsertDownstreamRoute(downstreamRoute);
        return context;
    }

    [Fact]
    public async Task GetResponse_ReturnsOkResponse_WhenMessageInvokerSucceeds()
    {
        // Arrange
        var context = CreateHttpContext();
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _messageInvokerPoolMock
            .Setup(p => p.Get(It.IsAny<DownstreamRoute>()))
            .Returns(_messageInvokerMock.Object);

        _messageInvokerMock
            .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.GetResponse(context);

        // Assert
        var okResponse = Assert.IsType<OkResponse<HttpResponseMessage>>(result);
        Assert.Equal(expectedResponse, okResponse.Data);
    }

    [Fact]
    public async Task GetResponse_ReturnsErrorResponse_WhenMessageInvokerThrows()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Test exception");
        var expectedError = new UnableToCompleteRequestError(new("mapped-error"));

        _messageInvokerPoolMock
            .Setup(p => p.Get(It.IsAny<DownstreamRoute>()))
            .Returns(_messageInvokerMock.Object);

        _messageInvokerMock
            .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _mapperMock.Setup(m => m.Map(exception)).Returns(expectedError);

        // Act
        var result = await _sut.GetResponse(context);

        // Assert
        var errorResponse = Assert.IsType<ErrorResponse<HttpResponseMessage>>(result);
        Assert.Contains(expectedError, errorResponse.Errors);
    }

    [Fact]
    public void Constructor_CreatesInstance_WhenDependenciesAreValid()
    {
        // Arrange
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var loggerMock = new Mock<IOcelotLogger>();
        loggerFactoryMock.Setup(f => f.CreateLogger<MessageInvokerHttpRequester>())
            .Returns(loggerMock.Object);

        var messageInvokerPoolMock = new Mock<IMessageInvokerPool>();
        var mapperMock = new Mock<IExceptionToErrorMapper>();

        // Act
        var sut = new MessageInvokerHttpRequester(
            loggerFactoryMock.Object,
            messageInvokerPoolMock.Object,
            mapperMock.Object);

        // Assert
        Assert.NotNull(sut);
        // Verify that the logger factory was used to create a logger
        loggerFactoryMock.Verify(f => f.CreateLogger<MessageInvokerHttpRequester>(), Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerFactoryIsNull()
    {
        // Arrange
        var messageInvokerPoolMock = new Mock<IMessageInvokerPool>();
        var mapperMock = new Mock<IExceptionToErrorMapper>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MessageInvokerHttpRequester(null!, messageInvokerPoolMock.Object, mapperMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMessageInvokerPoolIsNull()
    {
        // Arrange
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var mapperMock = new Mock<IExceptionToErrorMapper>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MessageInvokerHttpRequester(loggerFactoryMock.Object, null!, mapperMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenMapperIsNull()
    {
        // Arrange
        var loggerFactoryMock = new Mock<IOcelotLoggerFactory>();
        var messageInvokerPoolMock = new Mock<IMessageInvokerPool>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new MessageInvokerHttpRequester(loggerFactoryMock.Object, messageInvokerPoolMock.Object, null!));
    }
}
