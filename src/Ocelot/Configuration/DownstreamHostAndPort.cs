namespace Ocelot.Configuration
{
    public class DownstreamHostAndPort
    {
        public DownstreamHostAndPort(string scheme, string host, int port)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
        }

        public string Scheme { get; private set; }
        public string Host { get; private set; }
        public int Port { get; private set; }
    }
}
