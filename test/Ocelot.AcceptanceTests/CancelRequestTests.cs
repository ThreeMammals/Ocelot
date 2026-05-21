using Microsoft.AspNetCore.Http;

namespace Ocelot.AcceptanceTests;

/// <summary>
/// TODO This test must be moved to another feat namespace, probably to Requester or to Routing...
/// </summary>
public sealed class CancelRequestTests : Steps
{
    [BddfyFact]
    [Trait("Bug", "893")] // https://github.com/ThreeMammals/Ocelot/issues/893
    [Trait("PR", "1367")] // https://github.com/ThreeMammals/Ocelot/pull/1367
    public void ShouldAbortServiceWorkWhenCancellingTheRequest()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        var started = new Notifier("service work started notifier");
        var stopped = new Notifier("service work finished notifier");
        this
            .Given(x => GivenThereIsAServiceRunningOn(DownstreamUrl(port), started, stopped))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning())
            .When(x => WhenIAmGettingUrlAndWaitingForNotification(started))
            .Then(x => _ex.ShouldNotBeNull(null).ShouldBeOfType<TaskCanceledException>(null))
            .And(x => started.NotificationSent.ShouldBeTrue(null))
            .And(x => stopped.NotificationSent.ShouldBeFalse(null))
        .BDDfy();
    }

    private Exception _ex;
    private async Task WhenIAmGettingUrlAndWaitingForNotification(Notifier started)
    {
        var getting = WhenImGettingUrlOnTheApiGateway("/");
        var canceling = WhenIWaitForNotification(started).ContinueWith(Cancel);
        _ex = await Task.WhenAll(getting, canceling).ShouldThrowAsync<TaskCanceledException>();
    }

    private Task Cancel(Task t) => Task.Run(ocelotClient.CancelPendingRequests);

    private void GivenThereIsAServiceRunningOn(string baseUrl, Notifier startedNotifier, Notifier stoppedNotifier)
    {
        handler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
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
