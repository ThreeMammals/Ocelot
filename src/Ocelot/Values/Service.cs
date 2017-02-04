using System.Collections.Generic;

namespace Ocelot.Values
{
    public class Service
    {
        public Service(string name, 
            HostAndPort hostAndPort, 
            string id, 
            string version, 
            IEnumerable<string> tags)
        {
            Name = name;
            HostAndPort = hostAndPort;
            Id = id;
            Version = version;
            Tags = tags;
        }
        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public IEnumerable<string> Tags { get; private set; }

        public HostAndPort HostAndPort { get; private set; }
    }
}