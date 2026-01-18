using Ocelot.Requester;
using System.Reflection;

namespace Ocelot.UnitTests.Requester;

public sealed class TimeoutDelegatingHandlerTests : UnitTest
{
    [Fact]
    public async Task SendAsync_OnTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        int ms = 100;
        using var baseHandler = new SocketsHttpHandler();
        using var handler = new TimeoutDelegatingHandler(TimeSpan.FromMilliseconds(ms));
        handler.InnerHandler = baseHandler;

        var type = handler.GetType();
        var method = type.GetMethod("SendAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.nuget.org/");
        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        // Act
        var args = new object[] { request, cts.Token };
        Task<HttpResponseMessage> sendAsync() => (Task<HttpResponseMessage>)method.Invoke(handler, args);
        async Task sendAsyncAndWaitForTimeout(int delay)
        {
            await sendAsync();
            await Task.Delay(delay); // wait for Timeout event
        }

        // Assert
        ms += IsCiCd() ? 50 : 0;
        var ex = await Assert.ThrowsAsync<TimeoutException>(() => sendAsyncAndWaitForTimeout(ms));
    }
}
