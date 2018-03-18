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

        public List<HeaderFindAndReplace> Upstream { get; private set; }

        public List<HeaderFindAndReplace> Downstream { get; private set; }
        public List<AddHeader> AddHeadersToDownstream {get;private set;}
    }

    public class AddHeader
    {
        public AddHeader(string key, string value)
        {
            this.Key = key;
            this.Value = value;

        }
        public string Key { get; private set; }
        public string Value { get; private set; }
    }
}
