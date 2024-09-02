using System.Collections.Concurrent;

namespace Ocelot.AcceptanceTests;

public class ConcurrentSteps : Steps, IDisposable
{
    protected ConcurrentDictionary<int, HttpResponseMessage> _responses;
    protected Task[] _tasks;

    public ConcurrentSteps()
    {
        _responses = new();
        _tasks = Array.Empty<Task>();
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

    public Task[] WhenIGetUrlOnTheApiGatewayConcurrently(string url, int times)
    {
        _tasks = new Task[times];
        _responses = new(times, times);
        for (var i = 0; i < times; i++)
        {
            _tasks[i] = GetParallelResponse(url, i);
            _responses[i] = null;
        }

        Task.WaitAll(_tasks);
        return _tasks;
    }

    public Task[] WhenIGetUrlOnTheApiGatewayConcurrently(int times, params string[] urls)
    {
        _tasks = new Task[times];
        _responses = new(times, times);
        for (var i = 0; i < times; i++)
        {
            _tasks[i] = GetParallelResponse(urls[i % urls.Length], i);
            _responses[i] = null;
        }

        Task.WaitAll(_tasks);
        return _tasks;
    }

    public void ThenAllStatusCodesShouldBe(HttpStatusCode expected)
        => _responses.ShouldAllBe(response => response.Value.StatusCode == expected);

    private async Task GetParallelResponse(string url, int threadIndex)
    {
        var response = await _ocelotClient.GetAsync(url);

        //Thread.Sleep(_random.Next(40, 60));
        //var content = await response.Content.ReadAsStringAsync();
        //var counterValue = content.Contains(':')
        //    ? content.Split(':')[0] // let the first fragment is counter value
        //    : content;
        //int count = int.Parse(counterValue);
        //count.ShouldBeGreaterThan(0);
        _responses[threadIndex] = response;
    }
}
