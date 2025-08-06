using Ocelot.Requester;
using System.Reflection;

namespace Ocelot.UnitTests.Requester;

public sealed class TimeoutDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_OnTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        using var baseHandler = new SocketsHttpHandler();
        using var handler = new TimeoutDelegatingHandler(TimeSpan.FromMilliseconds(3));
        handler.InnerHandler = baseHandler;

        var type = handler.GetType();
        var method = type.GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.nuget.org/");
        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        // Act
        var args = new object[] { request, cts.Token };
        Task<HttpResponseMessage> sendAsync() => (Task<HttpResponseMessage>)method.Invoke(handler, args);
        var ex = await Assert.ThrowsAsync<TimeoutException>(sendAsync);

        // Assert
        Assert.NotNull(ex);
        Assert.IsType<TimeoutException>(ex);
    }
}
