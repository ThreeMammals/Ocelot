using System.Collections.Generic;

namespace Ocelot.Configuration.Creator
{
    public class HeaderTransformations
    {
        public HeaderTransformations(List<HeaderFindAndReplace> upstream, List<HeaderFindAndReplace> downstream)
        {
            Upstream = upstream;
            Downstream = downstream;
        }

        public List<HeaderFindAndReplace> Upstream {get;private set;}

        public List<HeaderFindAndReplace> Downstream {get;private set;}
    }
}
