using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Ocelot.Testing;

public static class PortFinder
{
    private const int EndPortRange = 45000;
    private static volatile int CurrentPort = 20000;
    private static readonly object SyncRoot = new();

    //private static readonly ConcurrentBag<int> UsedPorts = new();

    /// <summary>
    /// Gets a pseudo-random port from the range [<see cref="CurrentPort"/>, <see cref="EndPortRange"/>] for one testing scenario.
    /// </summary>
    /// <returns>New allocated port.</returns>
    /// <exception cref="ExceedingPortRangeException">Critical situation where available ports range has been exceeded.</exception>
    public static int GetRandomPort()
    {
        lock (SyncRoot)
        {
            ExceedingPortRangeException.ThrowIf(CurrentPort > EndPortRange);
            while (!TryUsePort(CurrentPort++));
            return CurrentPort++;
        }
    }

    /// <summary>
    /// Gets the exact number of ports from the range [<see cref="CurrentPort"/>, <see cref="EndPortRange"/>] for one testing scenario.
    /// </summary>
    /// <param name="count">The number of wanted ports.</param>
    /// <returns>Array of allocated ports.</returns>
    /// <exception cref="ExceedingPortRangeException">Critical situation where available ports range has been exceeded.</exception>
    public static int[] GetPorts(int count)
    {
        var ports = new int[count];
        for (int i = 0; i < count; i++)
        {
            ports[i] = GetRandomPort();
        }
        return ports;
    }

    private static bool TryUsePort(int port)
    {
        //UsedPorts.Add(port); // TODO Review or remove, now useless
        Socket? socket = null;
        try
        {
            var ipe = new IPEndPoint(IPAddress.Loopback, port);
            socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipe);
            socket.Close();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            socket?.Dispose();
        }
    }
}

public class ExceedingPortRangeException : Exception
{
    public ExceedingPortRangeException()
        : base("Cannot find available port to bind to!") { }

    public static void ThrowIf(bool condition)
        => _ = condition ? throw new ExceedingPortRangeException() : 0;
}
