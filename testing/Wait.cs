using System.Diagnostics;

namespace Ocelot.Testing;

public class Wait
{

    private readonly int _milliSeconds;
    public static Wait For(int milliSeconds) => new(milliSeconds);

    private Wait() { }
    private Wait(int milliSeconds)
    {
        _milliSeconds = milliSeconds;
    }

    public bool Until(Func<bool> condition, int delayMs = 100)
    {
        var watcher = Stopwatch.StartNew();
        while (watcher.ElapsedMilliseconds < _milliSeconds)
        {
            if (condition.Invoke())
            {
                watcher.Stop();
                return true;
            }
            Thread.Sleep(delayMs);
        }
        watcher.Stop();
        return false;
    }

    public async Task<bool> UntilAsync(Func<CancellationToken, Task<bool>> condition, CancellationToken token, int delayMs = 100)
    {
        var watcher = Stopwatch.StartNew();
        while (watcher.ElapsedMilliseconds < _milliSeconds)
        {
            if (await condition.Invoke(token))
            {
                watcher.Stop();
                return true;
            }
            await Task.Delay(delayMs, token);
        }
        watcher.Stop();
        return false;
    }
}
