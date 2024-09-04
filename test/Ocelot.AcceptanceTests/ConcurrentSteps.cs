using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ocelot.AcceptanceTests;

public class ConcurrentSteps : Steps, IDisposable
{
    protected Task[] _tasks;
    protected ServiceHandler[] _handlers;
    protected ConcurrentDictionary<int, HttpResponseMessage> _responses;
    protected Dictionary<int, int> _counters;
    protected static readonly object CountersSyncRoot = new();

    public ConcurrentSteps()
    {
        _tasks = Array.Empty<Task>();
        _handlers = Array.Empty<ServiceHandler>();
        _responses = new();
        _counters = new();
    }

    public override void Dispose()
    {
        foreach (var handler in _handlers)
        {
            handler?.Dispose();
        }

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
        _handlers = new ServiceHandler[1]; // allocate single instance
        _counters = new(1); // single counter
        GivenServiceIsRunning(url, response, 0, statusCode);
        _counters[0] = 0;
    }

    protected void GivenThereIsAServiceRunningOn(string url, string basePath, string responseBody)
    {
        var handler = new ServiceHandler();
        _handlers = new ServiceHandler[] { handler };
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
        _handlers = new ServiceHandler[urls.Length]; // allocate multiple instances
        _counters = new(urls.Length); // multiple counters
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
        _handlers[index] ??= new();
        var serviceHandler = _handlers[index];
        serviceHandler.GivenThereIsAServiceRunningOn(url, MapGet(index, response, successCode));
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

    protected RequestDelegate MapGet(int index, string responseBody) => MapGet(index, responseBody, HttpStatusCode.OK);
    protected RequestDelegate MapGet(int index, string responseBody, HttpStatusCode successCode) => async context =>
    {
        await Task.Delay(Random.Shared.Next(5, 15)); // emulate integration delay up to 15 milliseconds
        string response;
        try
        {
            lock (CountersSyncRoot)
            {
                int count = ++_counters[index];
                response = string.Concat(count, ':', responseBody);
            }

            context.Response.StatusCode = (int)successCode;
            await context.Response.WriteAsync(response);
        }
        catch (Exception exception)
        {
            response = string.Concat(1, ':', exception.StackTrace);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(response);
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

    private async Task GetParallelResponse(string url, int threadIndex)
    {
        var response = await _ocelotClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        var counterString = content.Contains(':')
            ? content.Split(':')[0] // let the first fragment is counter value
            : "0";
        int count = int.Parse(counterString);
        count.ShouldBeGreaterThan(0);
        _responses[threadIndex] = response;
    }

    public void ThenAllStatusCodesShouldBe(HttpStatusCode expected)
        => _responses.ShouldAllBe(response => response.Value.StatusCode == expected);

    private string CalledTimesMessage()
    {
        var sortedByIndex = _counters.OrderBy(_ => _.Key).Select(_ => _.Value).ToArray();
        return $"All values are [{string.Join(',', sortedByIndex)}]";
    }

    public void ThenAllServicesShouldHaveBeenCalledTimes(int expected)
        => _counters.Sum(_ => _.Value).ShouldBe(expected, CalledTimesMessage());

    public void ThenServiceShouldHaveBeenCalledTimes(int index, int expected)
        => _counters[index].ShouldBe(expected, CalledTimesMessage());

    public void ThenServicesShouldHaveBeenCalledTimes(params int[] expected)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            _counters[i].ShouldBe(expected[i], CalledTimesMessage());
        }
    }

    public void ThenAllServicesCalledRealisticAmountOfTimes(int bottom, int top)
    {
        var sortedByIndex = _counters.OrderBy(_ => _.Key).Select(_ => _.Value).ToArray();
        var customMessage = $"{nameof(bottom)}: {bottom}\n    {nameof(top)}: {top}\n    All values are [{string.Join(',', sortedByIndex)}]";
        int sum = 0, totalSum = _counters.Sum(_ => _.Value);

        // Last offline services cannot be called at all, thus don't assert zero counters
        for (int i = 0; i < _counters.Count && sum < totalSum; i++)
        {
            int actual = _counters[i];
            actual.ShouldBeInRange(bottom, top, customMessage);
            sum += actual;
        }
    }
}
