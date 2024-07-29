using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests;

public sealed class CancelRequestTests : Steps, IDisposable
{
    private readonly ServiceHandler _serviceHandler;

    public CancelRequestTests()
    {
        _serviceHandler = new ServiceHandler();
    }

    public override void Dispose()
    {
        _serviceHandler?.Dispose();
        base.Dispose();
    }

    [Fact]
    public async Task ShouldAbortServiceWork_WhenCancellingTheRequest()
    {
        // Arrange
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        var started = new Notifier("service work started notifier");
        var stopped = new Notifier("service work finished notifier");
        GivenThereIsAServiceRunningOn(DownstreamUrl(port), started, stopped);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();

        // Act: Initialize
        var getting = WhenIGetUrl("/");
        var canceling = WhenIWaitForNotification(started).ContinueWith(Cancel);
        Exception ex = null;

        // Act
        try
        {
            await Task.WhenAll(getting, canceling);
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Assert
        started.NotificationSent.ShouldBeTrue();
        stopped.NotificationSent.ShouldBeFalse();
#if NET8_0_OR_GREATER
        ex.ShouldNotBeNull().ShouldBeOfType<TaskCanceledException>();
#else
        ex.ShouldNotBeNull().ShouldBeOfType<OperationCanceledException>();
#endif
    }

    private Task Cancel(Task t) => Task.Run(_ocelotClient.CancelPendingRequests);

    private void GivenThereIsAServiceRunningOn(string baseUrl, Notifier startedNotifier, Notifier stoppedNotifier)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
        {
            startedNotifier.NotificationSent = true;
            await Task.Delay(SERVICE_WORK_TIME, context.RequestAborted);

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync("OK");
            stoppedNotifier.NotificationSent = true;
        });
    }

    private const int SERVICE_WORK_TIME = 1_000;
    private const int WAITING_TIME = 50;
    private const int MAX_WAITING_TIME = 10_000;

    private static async Task WhenIWaitForNotification(Notifier notifier)
    {
        int waitingTime = 0;
        while (!notifier.NotificationSent)
        {
            await Task.Delay(WAITING_TIME);
            waitingTime += WAITING_TIME;
            if (waitingTime > MAX_WAITING_TIME)
            {
                throw new TimeoutException(notifier.Name + $" did not sent notification within {MAX_WAITING_TIME / 1000} second(s).");
            }
        }
    }

    class Notifier
    {
        public Notifier(string name) => Name = name;
        public bool NotificationSent { get; set; }
        public string Name { get; set; }
    }
}
