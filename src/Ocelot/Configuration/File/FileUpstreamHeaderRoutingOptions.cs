using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileUpstreamHeaderRoutingOptions
    {
        public FileUpstreamHeaderRoutingOptions()
        {
            Headers = new Dictionary<string, List<string>>();
            CombinationMode = "";
        }

        public Dictionary<string, List<string>> Headers { get; set; }

        public string CombinationMode { get; set; }
    }
}
