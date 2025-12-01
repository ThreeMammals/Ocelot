using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.Provider.Polly;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.QualityOfService;

public class QosSteps : Steps, IQosSteps
{
    private readonly Steps self;
    public QosSteps(Steps self) => this.self = self;

    //public async Task TestRouteCircuitBreaker(FileRoute route, int index = 0, FileQoSOptions qos = null)
    public async Task TestRouteCircuitBreaker(int port, string upstreamPath, FileQoSOptions qos = null, int index = 0)
    {
        qos ??= /*route.QoSOptions ??*/ new();
        //int port = route.DownstreamHostAndPorts[0].Port;
        int count = PollyQoSResiliencePipelineProvider.DefaultServerErrorCodes.Count;
        HttpStatusCode[] codes = PollyQoSResiliencePipelineProvider.DefaultServerErrorCodes.ToArray();
        HttpStatusCode nextBadStatus = codes[DateTime.Now.Millisecond % count];
        GivenThereIsABrokenServiceRunningOn(port, nextBadStatus, index);
        for (int i = 0; qos.MinimumThroughput.HasValue && i < qos.MinimumThroughput.Value; i++)
        {
            nextBadStatus = codes[DateTime.Now.Millisecond % count];
            GivenThereIsABrokenServiceOnline(nextBadStatus, index);
            await self.WhenIGetUrlOnTheApiGateway(upstreamPath);//route.UpstreamPathTemplate);
            await self.ThenTheResponseShouldBeAsync(nextBadStatus, nextBadStatus.ToString());
        }
        GivenThereIsABrokenServiceOnline(HttpStatusCode.OK, index);
        if (qos.MinimumThroughput.HasValue && qos.MinimumThroughput > 0)
        {
            await self.WhenIGetUrlOnTheApiGateway(upstreamPath);//route.UpstreamPathTemplate);
            self.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // Circuit is open

            await GivenIWaitAsync(qos.BreakDuration.Value); // Wait until the circuit is either half-open or closed
            await self.WhenIGetUrlOnTheApiGateway(upstreamPath); //route.UpstreamPathTemplate);
            await self.ThenTheResponseShouldBeAsync(HttpStatusCode.OK, "OK");
        }
    }

    public async Task TestRouteTimeout(FileRoute route)
    {
        int counter = 0;
        bool notFailing() => false;
        int firstHasTimeout()
        {
            int count = Interlocked.Increment(ref counter),
                timeout = route.QoSOptions.Timeout.Value;
            return count <= 1 ? timeout + 100 : timeout / 2;
        }
        int port = route.DownstreamHostAndPorts[0].Port;
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK, firstHasTimeout, notFailing);
        await self.WhenIGetUrlOnTheApiGateway(route.UpstreamPathTemplate);
        self.ThenTheStatusCodeShouldBe(HttpStatusCode.ServiceUnavailable); // OnTimeout
        await self.WhenIGetUrlOnTheApiGateway(route.UpstreamPathTemplate);
        await self.ThenTheResponseShouldBeAsync(HttpStatusCode.OK);
    }

    public void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, Func<int> timeoutStrategy, Func<bool> failingStrategy, [CallerMemberName] string response = null)
    {
        Task MapBodyWithTimeout(HttpContext context)
        {
            int delayMs = timeoutStrategy();
            bool failed = failingStrategy();
            HttpStatusCode status = failed ? HttpStatusCode.InternalServerError : statusCode;
            context.Response.StatusCode = (int)status;
            return Task.Delay(delayMs)
                .ContinueWith(t => context.Response.WriteAsync(response));
        }
        handler.GivenThereIsAServiceRunningOn(port, MapBodyWithTimeout);
    }

    public HttpStatusCode[] BrokenServiceStatusCode { get; set; }
    public void GivenThereIsABrokenServiceRunningOn(int port, HttpStatusCode brokenStatusCode, int index = 0)
    {
        GivenThereIsABrokenServiceOnline(brokenStatusCode, index);
        handler.GivenThereIsAServiceRunningOn(port, async context =>
        {
            var code = BrokenServiceStatusCode[index];
            context.Response.StatusCode = (int)code;
            await context.Response.WriteAsync(code.ToString());
        });
    }

    public void GivenThereIsABrokenServiceOnline(HttpStatusCode onlineStatusCode, int index = 0, int length = 1)
    {
        BrokenServiceStatusCode ??= new HttpStatusCode[length];
        BrokenServiceStatusCode[index] = onlineStatusCode;
    }
}

public interface IQosSteps
{
    //Task TestRouteCircuitBreaker(FileRoute route, int index = 0, FileQoSOptions qos = null);
    Task TestRouteCircuitBreaker(int port, string upstreamPath, FileQoSOptions qos = null, int index = 0);
    Task TestRouteTimeout(FileRoute route);
    void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode,
        Func<int> timeoutStrategy, Func<bool> failingStrategy, [CallerMemberName] string response = null);
    HttpStatusCode[] BrokenServiceStatusCode { get; set; }
    void GivenThereIsABrokenServiceRunningOn(int port, HttpStatusCode brokenStatusCode, int index = 0);
    void GivenThereIsABrokenServiceOnline(HttpStatusCode onlineStatusCode, int index = 0, int length = 1);
}
