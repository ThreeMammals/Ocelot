namespace Ocelot.Configuration
{
    public class DownstreamAddress
    {
        public DownstreamAddress(string downstreamHost, int downstreamPort)
        {
            DownstreamHost = downstreamHost;
            DownstreamPort = downstreamPort;
        }
        public string DownstreamHost { get; private set; }
        public int DownstreamPort { get; private set; }
    }
}
