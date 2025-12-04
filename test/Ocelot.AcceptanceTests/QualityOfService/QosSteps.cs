using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Provider.Polly;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.QualityOfService;

public class QosSteps : Steps, IQosSteps
{
    private readonly Steps self;
    public QosSteps(Steps self) => this.self = self;

    public async Task TestRouteCircuitBreaker(int[] ports, string upstreamPath, FileQoSOptions qos, int index = 0, bool isDiscovery = false)
    {
        qos ??= new();
        await handler.ReleasePortAsync(ports)
            .ContinueWith(t => self.ReleasePortAsync(ports));
        int count = PollyQoSResiliencePipelineProvider.DefaultServerErrorCodes.Count;
        HttpStatusCode[] codes = PollyQoSResiliencePipelineProvider.DefaultServerErrorCodes.ToArray();
        HttpStatusCode nextBadStatus = codes[DateTime.Now.Millisecond % count];
        for (int i = 0; i < ports.Length; i++)
        {
            GivenThereIsABrokenServiceRunningOn(ports[i], nextBadStatus, index);
        }
        for (int i = 0; qos.MinimumThroughput.HasValue && i < qos.MinimumThroughput.Value; i++)
        {
            nextBadStatus = codes[DateTime.Now.Millisecond % count];
            GivenThereIsABrokenServiceOnline(nextBadStatus, index, isDiscovery: isDiscovery);
            await self.WhenIGetUrlOnTheApiGateway(upstreamPath);
            await self.ThenTheResponseShouldBeAsync(nextBadStatus, nextBadStatus.ToString());
        }
        if (qos.MinimumThroughput.HasValue && qos.MinimumThroughput > 0)
        {
            GivenThereIsABrokenServiceOnline(HttpStatusCode.OK, index, isDiscovery: isDiscovery);
            await self.WhenIGetUrlOnTheApiGateway(upstreamPath);
            self.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // Circuit is open

            GivenThereIsABrokenServiceOnline(HttpStatusCode.OK, index, isDiscovery: isDiscovery);
            await GivenIWaitAsync(qos.BreakDuration.Value); // Wait until the circuit is either half-open or closed
            await self.WhenIGetUrlOnTheApiGateway(upstreamPath);
            await self.ThenTheResponseShouldBeAsync(HttpStatusCode.OK, "OK");
        }
    }

    public async Task TestRouteTimeout(int[] ports, string upstreamPath, FileQoSOptions qos)
    {
        int counter = 0;
        bool notFailing() => false;
        int firstHasTimeout()
        {
            int count = Interlocked.Increment(ref counter),
                timeout = qos.Timeout.Value;
            return count <= 1 ? timeout + 100 : timeout / 2;
        }
        await handler.ReleasePortAsync(ports)
            .ContinueWith(t => self.ReleasePortAsync(ports));
        for (int i = 0; i < ports.Length; i++)
        {
            GivenThereIsAServiceRunningOn(ports[i], HttpStatusCode.OK, firstHasTimeout, notFailing);
        }
        await self.WhenIGetUrlOnTheApiGateway(upstreamPath);
        self.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // OnTimeout
        await self.WhenIGetUrlOnTheApiGateway(upstreamPath);
        await self.ThenTheResponseShouldBeAsync(HttpStatusCode.OK);
    }

    public Action<int> CounterStrategy { get; set; }

    public void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode,
        Func<int> timeoutStrategy, Func<bool> failingStrategy, [CallerMemberName] string response = null)
    {
        Task MapBodyWithTimeout(HttpContext context)
        {
            int delayMs = timeoutStrategy();
            bool failed = failingStrategy();
            HttpStatusCode status = failed ? HttpStatusCode.InternalServerError : statusCode;
            context.Response.StatusCode = (int)status;
            CounterStrategy?.Invoke(port);
            return Task.Delay(delayMs)
                .ContinueWith(t => context.Response.WriteAsync(response));
        }
        handler.GivenThereIsAServiceRunningOn(port, MapBodyWithTimeout);
    }

    public ConcurrentDictionary<int, HttpStatusCode> BrokenServiceStatusCode = new();
    public void GivenThereIsABrokenServiceRunningOn(int port, HttpStatusCode brokenStatusCode, int index = 0)
    {
        GivenThereIsABrokenServiceOnline(brokenStatusCode, index);
        handler.GivenThereIsAServiceRunningOn(port, context =>
        {
            var code = BrokenServiceStatusCode[index];
            context.Response.StatusCode = (int)code;
            CounterStrategy?.Invoke(port);
            return context.Response.WriteAsync(code.ToString());
        });
    }
    public void GivenThereIsABrokenServiceOnline(HttpStatusCode onlineStatusCode, int index = 0, int length = 1, bool isDiscovery = false)
    {
        if (!isDiscovery)
        {
            BrokenServiceStatusCode[index] = onlineStatusCode;
        }
        else
        {
            foreach (var kv in BrokenServiceStatusCode)
                BrokenServiceStatusCode[kv.Key] = onlineStatusCode;
        }
    }
}

public interface IQosSteps
{
    Task TestRouteCircuitBreaker(int[] ports, string upstreamPath, FileQoSOptions qos, int index = 0, bool isDiscovery = false);
    Task TestRouteTimeout(int[] ports, string upstreamPath, FileQoSOptions qos);
    void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode,
        Func<int> timeoutStrategy, Func<bool> failingStrategy, [CallerMemberName] string response = null);
    void GivenThereIsABrokenServiceRunningOn(int port, HttpStatusCode brokenStatusCode, int index = 0);
    void GivenThereIsABrokenServiceOnline(HttpStatusCode onlineStatusCode, int index = 0, int length = 1, bool isDiscovery = false);
}
