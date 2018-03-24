using System.Collections.Generic;

namespace Ocelot.Configuration.Creator
{
    public class HeaderTransformations
    {
        public HeaderTransformations(
            List<HeaderFindAndReplace> upstream, 
            List<HeaderFindAndReplace> downstream,
            List<AddHeader> addHeader)
        {
            AddHeadersToDownstream = addHeader;
            Upstream = upstream;
            Downstream = downstream;
        }

        public List<HeaderFindAndReplace> Upstream { get; }

        public List<HeaderFindAndReplace> Downstream { get; }

        public List<AddHeader> AddHeadersToDownstream { get; }
    }
}
