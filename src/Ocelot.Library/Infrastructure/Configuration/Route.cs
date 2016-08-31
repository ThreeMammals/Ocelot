namespace Ocelot.Library.Infrastructure.Configuration
{
    public class Route
    {
        public Route()
        {

        }
        public Route(string downstream, string upstream)
        {
            Downstream = downstream;
            Upstream = upstream;
        }

        public string Downstream { get; private set; }
        public string Upstream { get; private set; }
    }
}
