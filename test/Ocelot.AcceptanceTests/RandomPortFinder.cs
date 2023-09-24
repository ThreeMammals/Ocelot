using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Ocelot.AcceptanceTests
{
    public static class RandomPortFinder
    {
        private const int EndPortRange = 45000;
        private static int _currentPort = 20000;
        private static readonly object LockObj = new();
        private static readonly ConcurrentBag<int> UsedPorts = new();

        public static int GetRandomPort()
        {
            lock (LockObj)
            {
                if (_currentPort > EndPortRange)
                {
                    throw new Exception("Cannot find available port to bind to.");
                }

                var port = UsePort(_currentPort);
                _currentPort += 1;
                return port;
            }
        }

        private static int UsePort(int randomPort)
        {
            UsedPorts.Add(randomPort);

            var ipe = new IPEndPoint(IPAddress.Loopback, randomPort);

            using var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipe);
            socket.Close();
            return randomPort;
        }
    }
}
