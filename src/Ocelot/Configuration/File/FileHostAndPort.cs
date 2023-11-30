namespace Ocelot.Configuration.File
{
    public class FileHostAndPort
    {
        public FileHostAndPort() { }

        public FileHostAndPort(FileHostAndPort from)
        {
            Host = from.Host;
            Port = from.Port;
        }

        public FileHostAndPort(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; set; }
        public int Port { get; set; }
    }
}
