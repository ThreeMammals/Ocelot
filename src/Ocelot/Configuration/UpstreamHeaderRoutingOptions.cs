using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class UpstreamHeaderRoutingOptions
    {
        public UpstreamHeaderRoutingOptions(Dictionary<string, HashSet<string>> headers, UpstreamHeaderRoutingTriggerMode mode)
        {
            Headers = new UpstreamRoutingHeaders(headers);
            Mode = mode;
        }

        public bool Enabled() => Headers.Any();

        public UpstreamRoutingHeaders Headers { get; }

        public UpstreamHeaderRoutingTriggerMode Mode { get; }
    }
}
