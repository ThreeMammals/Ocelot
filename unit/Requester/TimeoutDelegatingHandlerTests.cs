using Ocelot.Requester;

namespace Ocelot.UnitTests.Requester;

public sealed class TimeoutDelegatingHandlerTests : UnitTest
{
    [Fact]
    public async Task SendAsync_OnTimeout_ShouldThrowTimeoutException()
    {
        // Arrange
        int ms = 100;
        using var baseHandler = new DelayedCancellationHandler();
        using var handler = new TimeoutDelegatingHandler(TimeSpan.FromMilliseconds(ms))
        {
            InnerHandler = baseHandler,
        };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        // Act, Assert
        await Assert.ThrowsAsync<TimeoutException>(() => invoker.SendAsync(request, token));
    }

    private sealed class DelayedCancellationHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously)
                .Task.WaitAsync(cancellationToken);
    }
}
