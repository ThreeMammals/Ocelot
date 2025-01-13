using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Ocelot.LoadBalancer.LoadBalancers;
using System.Diagnostics;

namespace Ocelot.AcceptanceTests.Core;

public sealed class LoadTests : ConcurrentSteps, IDisposable
{
    private readonly ServiceHandler _serviceHandler;
    private string _downstreamPath;
    private string _downstreamQuery;

    public LoadTests()
    {
        _serviceHandler = new();
    }

    public override void Dispose()
    {
        _serviceHandler.Dispose();
        base.Dispose();
    }

    [Fact]
    [Trait("Feat", "1348")]
    [Trait("Bug", "2246")]
    public async Task ShouldLoadRegexCachingHeavily_NoOutOfMemoryExceptions_NoMemoryLeaks()
    {
        var limit = Environment.GetEnvironmentVariable("DOTNET_GCHeapHardLimit");
        var port = PortFinder.GetRandomPort();
        var route = GivenRoute(port, "/my-gateway/order/{orderNumber}", "/order/{orderNumber}");
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning();
        GivenThereIsAServiceRunningOn(port, "/order/", "Hello from Donny");

        var currentProcess = Process.GetCurrentProcess();
        long memoryUsage = currentProcess.WorkingSet64;
        float memoryUsageMB = (float)memoryUsage / (1024 * 1024);

        // Step 1: Measure memory consumption for constant upstream URL
        GC.Collect();
        var a = GC.GetGCMemoryInfo();
        long step1TotalMemory = GC.GetTotalMemory(true);
        long step1TotalBytes = GC.GetTotalAllocatedBytes();
        long step1ThreadBytes = GC.GetAllocatedBytesForCurrentThread();

        //.When(x => WhenIGetUrlOnTheApiGatewayConcurrently("/", 50))
        //.When(x => RunParallelRequests(100_000, (i) => "/my-gateway/order/" + i))
        await WhenIDoActionMultipleTimes(10_000, (i) => WhenIGetUrlOnTheApiGateway("/my-gateway/order/1")); // const url

        GC.Collect();
        step1TotalMemory = GC.GetTotalMemory(true);
        step1TotalBytes = GC.GetTotalAllocatedBytes();
        step1ThreadBytes = GC.GetAllocatedBytesForCurrentThread();
        long memoryUsage1 = currentProcess.WorkingSet64;
        float memoryUsageMB1 = (float)memoryUsage / (1024 * 1024);

        await WhenIGetUrlOnTheApiGateway("/my-gateway/order/1");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseBodyShouldBe("Hello from Donny");

        // Step 2: Measure memory consumption for varying upstream URL
        GC.Collect();
        long step2TotalMemory = GC.GetTotalMemory(true);
        long step2TotalBytes = GC.GetTotalAllocatedBytes();
        long step2ThreadBytes = GC.GetAllocatedBytesForCurrentThread();

        await WhenIDoActionMultipleTimes(10_000, (i) => WhenIGetUrlOnTheApiGateway("/my-gateway/order/" + i)); // varying url

        GC.Collect();
        step2TotalMemory = GC.GetTotalMemory(true);
        step2TotalBytes = GC.GetTotalAllocatedBytes();
        step2ThreadBytes = GC.GetAllocatedBytesForCurrentThread();
        long memoryUsage2 = currentProcess.WorkingSet64;
        float memoryUsageMB2 = (float)memoryUsage / (1024 * 1024);
    }

    private async Task WhenIGetUrlOnTheApiGatewaySequentially(int i)
    {
        //int count = i + 1;
        await WhenIGetUrlOnTheApiGateway("/my-gateway/order/" + i);

        //ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        //ThenServiceShouldHaveBeenCalledTimes(0, count);
        //ThenTheResponseBodyShouldBe($"{count}:{Bug2119ServiceNames[0]}", $"i is {i}");
    }

    private FileRoute GivenRoute(int port, string upstream, string downstream) => new()
    {
        DownstreamPathTemplate = string.IsNullOrEmpty(downstream) ? "/" : downstream,
        DownstreamScheme = Uri.UriSchemeHttp,
        UpstreamPathTemplate = string.IsNullOrEmpty(upstream) ? "/" : upstream,
        UpstreamHttpMethod = new() { HttpMethods.Get },
        LoadBalancerOptions = new() { Type = nameof(NoLoadBalancer) },
        DownstreamHostAndPorts = new() { Localhost(port) },
    };

    private void GivenThereIsAServiceRunningOn(int port, string basePath, string responseBody)
    {
        var baseUrl = DownstreamUrl(port);
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, basePath, MapGet);

        async Task MapGet(HttpContext context)
        {
            _downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
                ? context.Request.PathBase.Value + context.Request.Path.Value
                : context.Request.Path.Value;
            _downstreamQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            var isOK = _downstreamPath.StartsWith(basePath);
            context.Response.StatusCode = isOK ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsync(isOK ? responseBody : nameof(HttpStatusCode.NotFound));
        }
    }
}
