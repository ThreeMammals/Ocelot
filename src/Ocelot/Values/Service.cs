namespace Ocelot.Values
{
    public class Service
    {
        public Service(string name, HostAndPort hostAndPort)
        {
            Name = name;
            HostAndPort = hostAndPort;
        }
        public string Name {get; private set;}
        public HostAndPort HostAndPort {get; private set;}
    }
}