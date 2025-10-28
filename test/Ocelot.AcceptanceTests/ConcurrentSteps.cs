using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ocelot.AcceptanceTests.LoadBalancer;
using Ocelot.Infrastructure.Extensions;
using Ocelot.LoadBalancer;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ocelot.AcceptanceTests;

public class ConcurrentSteps : Steps
{
    protected Task[] _tasks;
    protected ConcurrentDictionary<int, HttpResponseMessage> _responses;
    protected volatile int[] _counters;

    public ConcurrentSteps()
    {
        _tasks = Array.Empty<Task>();
        _responses = new();
        _counters = Array.Empty<int>();
    }

    public override void Dispose()
    {
        foreach (var response in _responses.Values)
        {
            response?.Dispose();
        }

        foreach (var task in _tasks)
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
        _counters = new int[1]; // single counter
        GivenServiceIsRunning(url, response, 0, statusCode);
        _counters[0] = 0;
    }

    protected void GivenThereIsAServiceRunningOn(string url, string basePath, string responseBody)
    {
        handler.GivenThereIsAServiceRunningOn(url, basePath, MapGet(basePath, responseBody));
    }

    protected void GivenMultipleServiceInstancesAreRunning(string[] urls, [CallerMemberName] string serviceName = null)
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
        _counters = new int[urls.Length]; // multiple counters
        for (int i = 0; i < urls.Length; i++)
        {
            GivenServiceIsRunning(urls[i], responses[i], i, statusCode);
            _counters[i] = 0;
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
        await context.Response.WriteAsync(isMatch ? responseBody : "Not Found");
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
        if (Volatile.Read(ref _counters[index]) > 0)
        {
            await Task.Delay(Random.Shared.Next(5, 15)); // emulate integration delay up to 15 milliseconds
        }

        string responseBody;
        var request = context.Request;
        var response = context.Response;
        try
        {
            int count = Interlocked.Increment(ref _counters[index]);
            responseBody = string.Concat(count, CounterSeparator, body);

            response.StatusCode = (int)successCode;
            response.Headers.Append(HeaderNames.ServiceIndex, new StringValues(index.ToString()));
            response.Headers.Append(HeaderNames.Host, new StringValues(request.Host.Host));
            response.Headers.Append(HeaderNames.Port, new StringValues(request.Host.Port.ToString()));
            response.Headers.Append(HeaderNames.Counter, new StringValues(count.ToString()));
            response.Headers.Append(HeaderNames.Path, new StringValues(request.Path + request.QueryString));
            await response.WriteAsync(responseBody);
        }
        catch (Exception exception)
        {
            responseBody = string.Concat(1, CounterSeparator, exception.StackTrace);
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await response.WriteAsync(responseBody);
        }
    };

    public Task[] WhenIGetUrlOnTheApiGatewayConcurrently(string url, int times)
        => RunParallelRequests(times, (i) => url);

    public Task[] WhenIGetUrlOnTheApiGatewayConcurrently(int times, params string[] urls)
        => RunParallelRequests(times, (i) => urls[i % urls.Length]);

    protected Task[] RunParallelRequests(int times, Func<int, string> urlFunc)
    {
        _tasks = new Task[times];
        _responses = new(times, times);
        for (var i = 0; i < times; i++)
        {
            var url = urlFunc(i);
            _tasks[i] = GetParallelResponse(url, i);
            _responses[i] = null;
        }

        Task.WaitAll(_tasks);
        return _tasks;
    }

    protected const string CounterSeparator = "^:^";
    private async Task GetParallelResponse(string url, int threadIndex)
    {
        var response = await ocelotClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        var counterString = content.Contains(CounterSeparator)
            ? content.Split(CounterSeparator)[0] // let the first fragment is counter value
            : "0";
        int count = int.Parse(counterString);
        count.ShouldBeGreaterThan(0);
        _responses[threadIndex] = response;
    }

    public void ThenAllStatusCodesShouldBe(HttpStatusCode expected)
        => _responses.ShouldAllBe(response => response.Value.StatusCode == expected);

    public void ThenAllResponseBodiesShouldBe(string expectedBody)
    {
        foreach (var r in _responses)
        {
            var content = r.Value.Content.ReadAsStringAsync().Result;
            content = content?.Contains(CounterSeparator) == true
                ? content.Split(CounterSeparator)[1] // remove counter for body comparison
                : "0";

            content.ShouldBe(expectedBody);
        }
    }
    public void ThenAllResponseBodiesShouldBe(int[] ports, string[] expected)
    {
        foreach (var r in _responses)
        {
            var response = r.Value;
            var portHeader = response.Headers.GetValues("Port").Csv();
            int port = int.Parse(portHeader);
            int i = Array.IndexOf(ports, port);
            var expectedBody = expected[i];
            var content = response.Content.ReadAsStringAsync().Result;
            content = content?.Contains(CounterSeparator) == true
                ? content.Split(CounterSeparator)[1] // remove counter for body comparison
                : "0";
            content.ShouldBe(expectedBody);
        }
    }

    protected string CalledTimesMessage()
        => $"All values are [{string.Join(',', _counters)}]";

    public void ThenAllServicesShouldHaveBeenCalledTimes(int expected)
        => _counters.Sum().ShouldBe(expected, CalledTimesMessage());

    public void ThenServiceShouldHaveBeenCalledTimes(int index, int expected)
        => _counters[index].ShouldBe(expected, CalledTimesMessage());

    public void ThenServicesShouldHaveBeenCalledTimes(params int[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            _counters[i].ShouldBe(expected[i], CalledTimesMessage());
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
            .AppendLine($"    All values are [{string.Join(',', _counters)}]")
            .ToString();
        int sum = 0, totalSum = _counters.Sum();

        // Last offline services cannot be called at all, thus don't assert zero counters
        for (int i = 0; i < _counters.Length && sum < totalSum; i++)
        {
            int actual = _counters[i];
            actual.ShouldBeInRange(bottom, top, customMessage);
            sum += actual;
        }
    }

    public void ThenAllServicesCalledOptimisticAmountOfTimes(ILoadBalancerAnalyzer analyzer)
    {
        if (analyzer == null) return;
        int bottom = analyzer.BottomOfConnections(),
            top = analyzer.TopOfConnections();
        ThenAllServicesCalledRealisticAmountOfTimes(bottom, top); // with unstable checkings
    }

    public void ThenServiceCountersShouldMatchLeasingCounters(ILoadBalancerAnalyzer analyzer, int[] ports, int totalRequests)
    {
        if (analyzer == null || ports == null)
            return;

        analyzer.ShouldNotBeNull().Analyze();
        analyzer.Events.Count.ShouldBe(totalRequests, $"{nameof(ILoadBalancerAnalyzer.ServiceName)}: {analyzer.ServiceName}");

        var leasingCounters = analyzer?.GetHostCounters() ?? new();
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
                    .AppendLine($"    Service counters: [{string.Join(',', _counters)}]")
                    .AppendLine($"    Leasing counters: [{string.Join(',', sortedLeasingCountersByPort)}]") // should have order of _counters
                    .ToString();
                int counter1 = _counters[i];
                int counter2 = leasingCounters[host];
                counter1.ShouldBe(counter2, customMessage);
            }
        }
    }

    protected IEnumerable<string> ThenAllResponsesHeaderExists(string key)
    {
        foreach (var kv in _responses)
        {
            var response = kv.Value.ShouldNotBeNull();
            response.Headers.Contains(key).ShouldBeTrue();
            var header = response.Headers.GetValues(key);
            yield return string.Join(';', header);
        }
    }

    protected virtual string ServiceName([CallerMemberName] string serviceName = null) => serviceName ?? GetType().Name;
    protected virtual string ServiceNamespace() => GetType().Namespace;
}
