namespace Ocelot.Values
{
    public class ServiceHostAndPort
    {
        public ServiceHostAndPort(string downstreamHost, int downstreamPort)
        {
            DownstreamHost = downstreamHost?.Trim('/');
            DownstreamPort = downstreamPort;
        }

        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
    }
}
