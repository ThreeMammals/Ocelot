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

    public bool Until(Func<bool> condition)
    {
        var watcher = Stopwatch.StartNew();
        while (watcher.ElapsedMilliseconds < _milliSeconds)
        {
            if (condition.Invoke())
            {
                watcher.Stop();
                return true;
            }
        }
        watcher.Stop();
        return false;
    }

    public async Task<bool> UntilAsync(Func<Task<bool>> condition)
    {
        var watcher = Stopwatch.StartNew();
        while (watcher.ElapsedMilliseconds < _milliSeconds)
        {
            if (await condition.Invoke())
            {
                watcher.Stop();
                return true;
            }
        }
        watcher.Stop();
        return false;
    }
}
