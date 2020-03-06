using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace Ocelot.AcceptanceTests
{
    public static class RandomPortFinder
    {
        private static readonly int TrialNumber = 100;
        private static readonly int BeginPortRange = 20000;
        private static readonly int EndPortRange = 45000;

        private static Random random = new Random();
        private static ConcurrentBag<int> usedPorts = new ConcurrentBag<int>();

        public static int GetRandomPort()
        {
            int randomPort = 0;
            for (int i = 0; i < TrialNumber; i++)
            {
                randomPort = random.Next(BeginPortRange, EndPortRange);
                if (usedPorts.Any(p => p == randomPort))
                {
                    continue;
                }
                else
                {
                    usedPorts.Add(randomPort);
                }

                try
                {
                    IPEndPoint ipe = new IPEndPoint(IPAddress.Loopback, randomPort);
                    using (var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.Bind(ipe);
                        socket.Close();
                        return randomPort;
                    }
                }
                catch (Exception)
                {
                    continue;
                }                
            }

            throw new Exception("Cannot find available port to bind to.");
        }
    }
}
