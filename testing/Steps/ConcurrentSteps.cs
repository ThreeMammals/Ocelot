using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer;
using Ocelot.Testing.LoadBalancer;
using Shouldly;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ocelot.Testing.Steps;

public class ConcurrentSteps : AcceptanceSteps
{
    protected Task[] Tasks;
    protected ConcurrentDictionary<int, HttpResponseMessage?> Responses;
    protected volatile int[] Counters;

    public ConcurrentSteps()
    {
        Tasks = [];
        Responses = [];
        Counters = [];
    }

    public override void Dispose()
    {
        foreach (var response in Responses.Values)
        {
            response?.Dispose();
        }

        foreach (var task in Tasks)
        {
            task?.Dispose();
        }

        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void GivenServiceInstanceIsRunning(string url, string response)
        => GivenServiceInstanceIsRunning(url, response, HttpStatusCode.OK);

    protected void GivenServiceInstanceIsRunning(string url, string response, HttpStatusCode statusCode)
    {
        Counters = new int[1]; // single counter
        GivenServiceIsRunning(url, response, 0, statusCode);
        Counters[0] = 0;
    }

    protected void GivenThereIsAServiceRunningOn(string url, string basePath, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(url, basePath, MapGet(basePath, responseBody));
    }

    protected void GivenMultipleServiceInstancesAreRunning(string[] urls, [CallerMemberName] string? serviceName = null)
    {
        serviceName ??= new Uri(urls[0]).Host;
        string[] responses = urls.Select(u => $"{serviceName}|url({u})").ToArray();
        GivenMultipleServiceInstancesAreRunning(urls, responses, HttpStatusCode.OK);
    }

    protected void GivenMultipleServiceInstancesAreRunning(string[] urls, string[] responses)
        => GivenMultipleServiceInstancesAreRunning(urls, responses, HttpStatusCode.OK);

    protected void GivenMultipleServiceInstancesAreRunning(string[] urls, string[] responses, HttpStatusCode statusCode)
    {
        Debug.Assert(urls.Length == responses.Length, "Length mismatch!");
        Counters = new int[urls.Length]; // multiple counters
        for (int i = 0; i < urls.Length; i++)
        {
            GivenServiceIsRunning(urls[i], responses[i], i, statusCode);
            Counters[i] = 0;
        }
    }
    protected void GivenMultipleServiceInstancesAreRunning(string[] urls, string[] responses, HttpStatusCode[] codes)
    {
        Debug.Assert(urls.Length == responses.Length, "Length mismatch!");
        Debug.Assert(urls.Length == codes.Length, "Length mismatch!");
        Debug.Assert(responses.Length == codes.Length, "Length mismatch!");
        Counters = new int[urls.Length]; // multiple counters
        for (int i = 0; i < urls.Length; i++)
        {
            GivenServiceIsRunning(urls[i], responses[i], i, codes[i]);
            Counters[i] = 0;
        }
    }

    private void GivenServiceIsRunning(string url, string response)
        => GivenServiceIsRunning(url, response, 0, HttpStatusCode.OK);
    private void GivenServiceIsRunning(string url, string response, int index)
        => GivenServiceIsRunning(url, response, index, HttpStatusCode.OK);

    private void GivenServiceIsRunning(string url, string response, int index, HttpStatusCode successCode)
    {
        response ??= successCode.ToString();
        handler.GivenThereIsAServiceRunningOn(url, MapGet(index, response, successCode));
    }

    protected static RequestDelegate MapGet(string path, string responseBody) => MapGet(path, responseBody, HttpStatusCode.OK);
    protected static RequestDelegate MapGet(string path, string responseBody, HttpStatusCode statusCode) => async context =>
    {
        var downstreamPath = !string.IsNullOrEmpty(context.Request.PathBase.Value)
            ? context.Request.PathBase.Value
            : context.Request.Path.Value;
        bool isMatch = downstreamPath == path;
        context.Response.StatusCode = (int)(isMatch ? statusCode : HttpStatusCode.NotFound);
        await context.Response.WriteAsync(isMatch ? responseBody : "Not Found", context.RequestAborted);
    };

    public static class HeaderNames
    {
        public const string ServiceIndex = nameof(LeaseEventArgs.ServiceIndex);
        public const string Host = nameof(Uri.Host);
        public const string Port = nameof(Uri.Port);
        public const string Counter = nameof(Counter);
        public const string Path = nameof(Path);
    }

    protected RequestDelegate MapGet(int index, string body) => MapGet(index, body, HttpStatusCode.OK);
    protected RequestDelegate MapGet(int index, string body, HttpStatusCode successCode) => async context =>
    {
        // Don't delay during the first service call
        if (Volatile.Read(ref Counters[index]) > 0)
        {
            await Task.Delay(Random.Shared.Next(5, 15)); // emulate integration delay up to 15 milliseconds
        }

        string responseBody;
        var request = context.Request;
        var response = context.Response;
        try
        {
            int count = Interlocked.Increment(ref Counters[index]);
            responseBody = string.Concat(count, CounterSeparator, body);

            response.StatusCode = (int)successCode;
            response.Headers.Append(HeaderNames.ServiceIndex, new StringValues(index.ToString()));
            response.Headers.Append(HeaderNames.Host, new StringValues(request.Host.Host));
            response.Headers.Append(HeaderNames.Port, new StringValues(request.Host.Port.ToString()));
            response.Headers.Append(HeaderNames.Counter, new StringValues(count.ToString()));
            response.Headers.Append(HeaderNames.Path, new StringValues(request.Path + request.QueryString));
            await response.WriteAsync(responseBody, context.RequestAborted);
        }
        catch (Exception exception)
        {
            responseBody = string.Concat(1, CounterSeparator, exception.StackTrace);
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await response.WriteAsync(responseBody, context.RequestAborted);
        }
    };

    public Task[] WhenIGetUrlOnTheApiGatewayConcurrently(string url, int times)
        => RunParallelRequests(times, (i) => url);

    public Task[] WhenIGetUrlOnTheApiGatewayConcurrently(int times, params string[] urls)
        => RunParallelRequests(times, (i) => urls[i % urls.Length]);

    protected Task[] RunParallelRequests(int times, Func<int, string> urlFunc)
    {
        Tasks = new Task[times];
        Responses = new(times, times);
        for (var i = 0; i < times; i++)
        {
            var url = urlFunc(i);
            Tasks[i] = GetParallelResponse(url, i);
            Responses[i] = null;
        }

        Task.WaitAll(Tasks, CancelMe);
        return Tasks;
    }

    protected const string CounterSeparator = "^:^";
    private async Task GetParallelResponse(string url, int threadIndex)
    {
        var response = await ocelotClient!.GetAsync(url, CancelMe);
        var content = await response.Content.ReadAsStringAsync(CancelMe);
        var counterString = content.Contains(CounterSeparator)
            ? content.Split(CounterSeparator)[0] // let the first fragment is counter value
            : "0";
        int count = int.Parse(counterString);
        if (content.IsNotEmpty()) count.ShouldBeGreaterThan(0);
        Responses[threadIndex] = response;
    }

    public void ThenAllStatusCodesShouldBe(HttpStatusCode expected)
        => Responses.ShouldAllBe(response => response.Value!.StatusCode == expected);

    public void ThenAllResponseBodiesShouldBe(string expectedBody)
    {
        foreach (var r in Responses)
        {
            var content = r.Value?.Content.ReadAsStringAsync(CancelMe).Result;
            content = content?.Contains(CounterSeparator) == true
                ? content.Split(CounterSeparator)[1] // remove counter for body comparison
                : "0";

            content.ShouldBe(expectedBody);
        }
    }
    public void ThenAllResponseBodiesShouldBe(int[] ports, string[] expected)
    {
        foreach (var r in Responses)
        {
            var response = r.Value.ShouldNotBeNull();
            var portHeader = response.Headers.GetValues("Port").Csv() ?? string.Empty;
            int port = int.Parse(portHeader);
            int i = Array.IndexOf(ports, port);
            var expectedBody = expected[i];
            var content = response.Content.ReadAsStringAsync(CancelMe).Result;
            content = content?.Contains(CounterSeparator) == true
                ? content.Split(CounterSeparator)[1] // remove counter for body comparison
                : "0";
            content.ShouldBe(expectedBody);
        }
    }

    protected string CalledTimesMessage()
        => $"All values are [{Counters.Csv()}]";

    public void ThenAllServicesShouldHaveBeenCalledTimes(int expected)
        => Counters.Sum().ShouldBe(expected, CalledTimesMessage());

    public void ThenServiceShouldHaveBeenCalledTimes(int index, int expected)
        => Counters[index].ShouldBe(expected, CalledTimesMessage());

    public void ThenServicesShouldHaveBeenCalledTimes(params int[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            Counters[i].ShouldBe(expected[i], CalledTimesMessage());
        }
    }

    public static int Bottom(int totalRequests, int totalServices)
        => totalRequests / totalServices;
    public static int Top(int totalRequests, int totalServices)
    {
        int bottom = Bottom(totalRequests, totalServices);
        return totalRequests - (bottom * totalServices) + bottom;
    }

    public void ThenAllServicesCalledRealisticAmountOfTimes(int bottom, int top)
    {
        var customMessage = new StringBuilder()
            .AppendLine($"{nameof(bottom)}: {bottom}")
            .AppendLine($"    {nameof(top)}: {top}")
            .AppendLine($"    All values are [{Counters.Csv()}]")
            .ToString();
        int sum = 0, totalSum = Counters.Sum();

        // Last offline services cannot be called at all, thus don't assert zero counters
        for (int i = 0; i < Counters.Length && sum < totalSum; i++)
        {
            int actual = Counters[i];
            actual.ShouldBeInRange(bottom, top, customMessage);
            sum += actual;
        }
    }

    public void ThenAllServicesCalledOptimisticAmountOfTimes(ILoadBalancerAnalyzer analyzer)
    {
        if (analyzer == null) return;
        int bottom = analyzer.BottomOfConnections(),
            top = analyzer.TopOfConnections();
        bottom = Math.Min(bottom, Counters.Min());
        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top); // with unstable checkings
    }

    public void ThenServiceCountersShouldMatchLeasingCounters(ILoadBalancerAnalyzer analyzer, int[] ports, int totalRequests)
    {
        if (analyzer == null || ports == null)
            return;

        analyzer.ShouldNotBeNull().Analyze();
        analyzer.Events.Count.ShouldBe(totalRequests, $"{nameof(ILoadBalancerAnalyzer.ServiceName)}: {analyzer.ServiceName}");

        var leasingCounters = analyzer.GetHostCounters() ?? [];
        var sortedLeasingCountersByPort = ports.Select(port => leasingCounters.FirstOrDefault(kv => kv.Key.DownstreamPort == port).Value).ToArray();
        for (int i = 0; i < ports.Length; i++)
        {
            var host = leasingCounters.Keys.FirstOrDefault(k => k.DownstreamPort == ports[i]);

            // Leasing info/counters can be absent because of offline service instance with exact port in unstable scenario
            if (host != null)
            {
                var customMessage = new StringBuilder()
                    .AppendLine($"{nameof(ILoadBalancerAnalyzer.ServiceName)}: {analyzer.ServiceName}")
                    .AppendLine($"    Port: {ports[i]}")
                    .AppendLine($"    Host: {host}")
                    .AppendLine($"    Service counters: [{Counters.Csv()}]")
                    .AppendLine($"    Leasing counters: [{sortedLeasingCountersByPort.Csv()}]") // should have order of _counters
                    .ToString();
                int counter1 = Counters[i];
                int counter2 = leasingCounters[host];
                counter1.ShouldBe(counter2, customMessage);
            }
        }
    }

    protected IEnumerable<string> ThenAllResponsesHeaderExists(string key)
    {
        foreach (var kv in Responses)
        {
            var response = kv.Value.ShouldNotBeNull();
            response.Headers.Contains(key).ShouldBeTrue();
            var header = response.Headers.GetValues(key);
            yield return string.Join(';', header);
        }
    }
}
