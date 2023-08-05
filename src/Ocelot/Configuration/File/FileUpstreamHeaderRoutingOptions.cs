using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileUpstreamHeaderRoutingOptions
    {
        public FileUpstreamHeaderRoutingOptions()
        {
            Headers = new Dictionary<string, IList<string>>();
            TriggerOn = string.Empty;
        }

        public IDictionary<string, IList<string>> Headers { get; set; }

        public string TriggerOn { get; set; }
    }
}
