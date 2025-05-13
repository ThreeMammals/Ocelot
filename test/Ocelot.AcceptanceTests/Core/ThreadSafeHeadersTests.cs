using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests.Core;

// Old integration tests
public sealed class ThreadSafeHeadersTests : Steps
{
    private readonly ConcurrentBag<ThreadSafeHeadersTestResult> _results;

    public ThreadSafeHeadersTests()
    {
        _results = new();
    }

    [Fact]
    public void Should_return_same_response_for_each_different_header_under_load_to_downsteam_service()
    {
        var port = PortFinder.GetRandomPort();
        var route = GivenDefaultRoute(port);
        var configuration = GivenConfiguration(route);
        GivenThereIsAConfiguration(configuration);
        GivenThereIsAServiceRunningOn(port, HttpStatusCode.OK);
        GivenOcelotIsRunning();
        WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues("/", 300);
        ThenTheSameHeaderValuesAreReturnedByTheDownstreamService();
    }
    private void GivenThereIsAServiceRunningOn(int port, HttpStatusCode statusCode, [CallerMemberName] string headerKey = nameof(ThreadSafeHeadersTests))
    {
        Task MapGet(HttpContext context)
        {
            var header = context.Request.Headers[headerKey];
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(header[0]);
        }
        handler.GivenThereIsAServiceRunningOn(port, MapGet);
    }

    private void WhenIGetUrlOnTheApiGatewayMultipleTimesWithDifferentHeaderValues(string url, int times, [CallerMemberName] string headerKey = nameof(ThreadSafeHeadersTests))
    {
        var tasks = new Task[times];
        for (var i = 0; i < times; i++)
        {
            var urlCopy = url;
            var rint = random.Next(0, 50);
            tasks[i] = GetForThreadSafeHeadersTest(urlCopy, rint, headerKey);
        }

        Task.WaitAll(tasks);
    }

    private async Task GetForThreadSafeHeadersTest(string url, int random, string headerKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(headerKey, [ random.ToString() ]);
        var response = await ocelotClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var result = int.Parse(content);
        var tshtr = new ThreadSafeHeadersTestResult(result, random);
        _results.Add(tshtr);
    }

    private void ThenTheSameHeaderValuesAreReturnedByTheDownstreamService()
    {
        foreach (var result in _results)
        {
            result.Result.ShouldBe(result.Random);
        }
    }

    private class ThreadSafeHeadersTestResult
    {
        public ThreadSafeHeadersTestResult(int result, int random)
        {
            Result = result;
            Random = random;
        }

        public int Result { get; }
        public int Random { get; }
    }
}
