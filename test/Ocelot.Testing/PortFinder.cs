using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Ocelot.Testing;

public static class PortFinder
{
    private const int EndPortRange = 45000;
    private static int CurrentPort = 20000;
    private static readonly object LockObj = new();
    private static readonly ConcurrentBag<int> UsedPorts = new();

    /// <summary>
    /// Gets a pseudo-random port from the range [<see cref="CurrentPort"/>, <see cref="EndPortRange"/>] for one testing scenario.
    /// </summary>
    /// <returns>New allocated port.</returns>
    /// <exception cref="ExceedingPortRangeException">Critical situation where available ports range has been exceeded.</exception>
    public static int GetRandomPort()
    {
        lock (LockObj)
        {
            ExceedingPortRangeException.ThrowIf(CurrentPort > EndPortRange);
            return UsePort(CurrentPort++);
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
        lock (LockObj)
        {
            for (int i = 0; i < count; i++, CurrentPort++)
            {
                ExceedingPortRangeException.ThrowIf(CurrentPort > EndPortRange);
                ports[i] = UsePort(CurrentPort);
            }
        }
        return ports;
    }

    private static int UsePort(int port)
    {
        UsedPorts.Add(port); // TODO Review or remove, now useless

        var ipe = new IPEndPoint(IPAddress.Loopback, port);

        using var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(ipe);
        socket.Close();
        return port;
    }
}

public class ExceedingPortRangeException : Exception
{
    public ExceedingPortRangeException()
        : base("Cannot find available port to bind to!") { }

    public static void ThrowIf(bool condition)
        => _ = condition ? throw new ExceedingPortRangeException() : 0;
}
