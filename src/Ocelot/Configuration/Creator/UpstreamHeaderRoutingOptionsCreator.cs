using System;
using System.Linq;
using System.Collections.Generic;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class UpstreamHeaderRoutingOptionsCreator : IUpstreamHeaderRoutingOptionsCreator
    {
        public UpstreamHeaderRoutingOptions Create(FileUpstreamHeaderRoutingOptions options)
        {
            UpstreamHeaderRoutingCombinationMode mode = UpstreamHeaderRoutingCombinationMode.Any;
            if (options.CombinationMode.Length > 0)
            {
                mode = (UpstreamHeaderRoutingCombinationMode)
                    Enum.Parse(typeof(UpstreamHeaderRoutingCombinationMode), options.CombinationMode, true);
            }

            Dictionary<string, HashSet<string>> headers = options.Headers.ToDictionary(kv => kv.Key.ToLowerInvariant(), kv => kv.Value);

            return new UpstreamHeaderRoutingOptions(headers, mode);
        }
    }
}
