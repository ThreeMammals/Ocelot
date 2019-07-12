using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class UpstreamHeaderRoutingOptions
    {
        public UpstreamHeaderRoutingOptions(Dictionary<string, HashSet<string>> headers, UpstreamHeaderRoutingCombinationMode mode)
        {
            Headers = new UpstreamRoutingHeaders(headers);
            Mode = mode;
        }

        public UpstreamRoutingHeaders Headers { get; private set; }

        public UpstreamHeaderRoutingCombinationMode Mode { get; private set; }
    }
}
