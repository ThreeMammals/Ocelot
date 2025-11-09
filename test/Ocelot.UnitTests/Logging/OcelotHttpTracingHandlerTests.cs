using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using System.Reflection;

namespace Ocelot.UnitTests.Logging;

public class OcelotHttpTracingHandlerTests : UnitTest
{
    private OcelotHttpTracingHandler handler;
    private readonly Mock<IOcelotTracer> tracer = new();
    private readonly Mock<IRequestScopedDataRepository> repo = new();
    private readonly Mock<HttpMessageHandler> httpMessageHandler = new();
    public OcelotHttpTracingHandlerTests()
    {
        handler = new(tracer.Object, repo.Object, httpMessageHandler.Object);
    }

    [Fact]
    public void Ctor()
    {
        // Arrange, Act
        handler = new(tracer.Object, repo.Object, httpMessageHandler.Object);

        // Assert
        Assert.NotNull(handler.InnerHandler);
        Assert.True(ReferenceEquals(httpMessageHandler.Object, handler.InnerHandler));
    }

    [Fact]
    public void Ctor_NullChecks()
    {
        // Arrange, Act, Assert: argument 1
        var ex = Assert.Throws<ArgumentNullException>(
            () => handler = new(null, repo.Object, httpMessageHandler.Object));
        Assert.Equal(nameof(tracer), ex.ParamName);

        // Arrange, Act, Assert: argument 2
        ex = Assert.Throws<ArgumentNullException>(
            () => handler = new(tracer.Object, null, httpMessageHandler.Object));
        Assert.Equal(nameof(repo), ex.ParamName);

        // Arrange, Act, Assert: argument 3
        handler = new(tracer.Object, repo.Object, null);
        Assert.NotNull(handler.InnerHandler);
        Assert.IsType<HttpClientHandler>(handler.InnerHandler);
    }

    [Fact]
    public async Task SendAsync()
    {
        // Arrange
        var sendAsync = handler.GetType().GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        HttpRequestMessage request = new();
        CancellationToken token = CancellationToken.None;
        HttpResponseMessage responseMessage = new();
        tracer.Setup(x => x.SendAsync(request,
            It.IsAny<Action<string>>(),
            It.IsAny<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMessage);
        repo.Setup(x => x.Add<string>(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new OkResponse());

        // Act
        var task = sendAsync.Invoke(handler, [request, token]) as Task<HttpResponseMessage>;
        var actual = await task;

        // Assert
        Assert.NotNull(actual);
        Assert.Same(responseMessage, actual);
        tracer.Verify(x => x.SendAsync(request, It.IsAny<Action<string>>(), It.IsAny<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        repo.Verify(x => x.Add<string>(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void AddTraceId()
    {
        // Arrange
        var addTraceId = handler.GetType().GetMethod(nameof(AddTraceId), BindingFlags.Instance | BindingFlags.NonPublic);
        Tuple<string, string> added = null;
        repo.Setup(x => x.Add<string>(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((k, v) => added = new(k, v))
            .Returns(new OkResponse());

        // Act
        addTraceId.Invoke(handler, [TestID]);

        // Assert
        repo.Verify(x => x.Add<string>(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
        Assert.NotNull(added);
        Assert.Equal("TraceId", added.Item1);
        Assert.Equal(TestID, added.Item2);
    }
}
