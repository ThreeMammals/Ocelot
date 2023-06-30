namespace Ocelot.Configuration
{
    public class DownstreamHostAndPort
    {
        public DownstreamHostAndPort(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }
        public int Port { get; }
    }
}
