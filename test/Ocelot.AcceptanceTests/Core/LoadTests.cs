using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using System.Diagnostics;

namespace Ocelot.AcceptanceTests.Core;

/// <summary>
/// TODO Move to separate Performance Testing (load testing) project.
/// </summary>
[Collection(nameof(SequentialTests))]
public sealed class LoadTests : ConcurrentSteps
{
    private string _downstreamPath;
    private string _downstreamQuery;

    public LoadTests()
    { }

    [Fact(Skip = "It should be moved to a separate project. It should be run during release only as an extra check for quality gates.")]
    [Trait("Feat", "1348")]
    [Trait("Bug", "2246")]
    public async Task ShouldLoadRegexCachingHeavily_NoMemoryLeaks()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/my-gateway/order/{orderNumber}", "/order/{orderNumber}");
        route.LoadBalancerOptions.Type = nameof(NoLoadBalancer);

        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenThereIsAServiceRunningOn(port, "/order/", "Hello from Donny");

        // Step 1: Measure memory consumption for constant upstream URL
        GC.Collect();
        var a = GC.GetGCMemoryInfo();
        var totalMemory = ToMegabytes(GC.GetTotalMemory(true));
        var currentProcess = Process.GetCurrentProcess();
        var memoryUsage = ToMegabytes(currentProcess.WorkingSet64);

        // Perform 50% of job for stable indicators
        await WhenIDoActionMultipleTimes(5_000, (i) => WhenIGetUrlOnTheApiGateway("/my-gateway/order/1")); // const url
        GC.Collect();
        var totalMemory0 = ToMegabytes(GC.GetTotalMemory(true));
        var process0 = Process.GetCurrentProcess();
        var memoryUsage0 = ToMegabytes(process0.WorkingSet64);

        await WhenIDoActionMultipleTimes(5_000, (i) => WhenIGetUrlOnTheApiGateway("/my-gateway/order/1")); // const url

        await WhenIGetUrlOnTheApiGateway("/my-gateway/order/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Donny");

        GC.Collect();
        var totalMemory1 = ToMegabytes(GC.GetTotalMemory(true));
        var process1 = Process.GetCurrentProcess();
        var memoryUsage1 = ToMegabytes(process1.WorkingSet64);

        // Step 2: Measure memory consumption for varying upstream URL
        // await WhenIDoActionForTime(TimeSpan.FromSeconds(30), (i) => WhenIGetUrlOnTheApiGatewayWithRequestId("/my-gateway/order/" + i)); // varying url
        await WhenIDoActionMultipleTimes(10_000, (i) => WhenIGetUrlOnTheApiGateway("/my-gateway/order/" + i)); // varying url

        GC.Collect();
        var totalMemory2 = ToMegabytes(GC.GetTotalMemory(true));
        var process2 = Process.GetCurrentProcess();
        var memoryUsage2 = ToMegabytes(process2.WorkingSet64);

        // Assert
        AssertDelta(totalMemory0, totalMemory1, totalMemory2);
        AssertDelta(memoryUsage0, memoryUsage1, memoryUsage2);
    }

    private static float ToMegabytes(long total) => (float)total / (1024 * 1024);

    private static void AssertDelta(float value0, float value1, float value2)
    {
        if (value1 <= value0)
            return;

        var delta = value1 - value0;
        if (value2 <= value1)
            return;

        var delta2 = value2 - value1;
        Assert.True(delta2 <= delta); // delta is not growing
    }

    private void GivenThereIsAServiceRunningOn(int port, string basePath, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(port, basePath, MapGet);

        Task MapGet(HttpContext context)
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value + context.Request.Path.Value
                : context.Request.Path.Value;
            _downstreamQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            var isOK = _downstreamPath.StartsWith(basePath);
            context.Response.StatusCode = isOK ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound;
            return context.Response.WriteAsync(isOK ? responseBody : nameof(HttpStatusCode.NotFound));
        }
    }
}
