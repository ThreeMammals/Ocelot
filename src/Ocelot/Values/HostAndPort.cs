namespace Ocelot.Values
{
    public class HostAndPort
    {
        public HostAndPort(string downstreamHost, int downstreamPort)
        {
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
        }

        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }

        public override string ToString()
        {
            return $"{DownstreamHost}:{DownstreamPort}";
        }
    }
}