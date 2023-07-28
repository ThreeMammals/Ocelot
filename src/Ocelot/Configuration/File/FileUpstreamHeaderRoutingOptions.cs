using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileUpstreamHeaderRoutingOptions
    {
        public FileUpstreamHeaderRoutingOptions()
        {
            Headers = new();
            TriggerOn = string.Empty;
        }

        public Dictionary<string, List<string>> Headers { get; set; }

        public string TriggerOn { get; set; }
    }
}
