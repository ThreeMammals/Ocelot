using System.Net.Sockets;
using System.Net;

namespace Ocelot.Testing
{
    public class TcpPortFinder
    {
        public static int FindAvailablePort()
        {
            TcpListener? listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener?.Stop();
            }
        }
    }
}
