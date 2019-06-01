namespace Ocelot.Values
{
    public class ServiceHostAndPort
    {
        public ServiceHostAndPort(string downstreamHost, int downstreamPort)
        {
            DownstreamHost = downstreamHost?.Trim('/');
            DownstreamPort = downstreamPort;
        }

        public string DownstreamHost { get; }

        public int DownstreamPort { get; }
    }
}
