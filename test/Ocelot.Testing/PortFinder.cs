using System;
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
    /// Gets a pseudo-random port from the range [<see cref="CurrentPort"/>, <see cref="EndPortRange"/>].
    /// </summary>
    /// <returns>New allocated port for testing scenario.</returns>
    /// <exception cref="ExceedingPortRangeException">Critical situation where available ports range has been exceeded.</exception>
    public static int GetRandomPort()
    {
        lock (LockObj)
        {
            if (CurrentPort > EndPortRange)
            {
                throw new ExceedingPortRangeException();
            }

            return UsePort(CurrentPort++);
        }
    }

    private static int UsePort(int port)
    {
        UsedPorts.Add(port);

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
}
