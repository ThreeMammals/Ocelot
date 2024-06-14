namespace Ocelot.Configuration.File
{
    public class FileHttpHandlerOptions
    {
        public FileHttpHandlerOptions()
        {
            AllowAutoRedirect = false;
            MaxConnectionsPerServer = int.MaxValue;
            UseCookieContainer = false;
            UseProxy = true;
            PooledConnectionLifetimeSeconds = null;
            UseDefaultCredentials = false;
        }

        public FileHttpHandlerOptions(FileHttpHandlerOptions from)
        {
            AllowAutoRedirect = from.AllowAutoRedirect;
            MaxConnectionsPerServer = from.MaxConnectionsPerServer;
            UseCookieContainer = from.UseCookieContainer;
            UseProxy = from.UseProxy;
            PooledConnectionLifetimeSeconds = from.PooledConnectionLifetimeSeconds;
            UseDefaultCredentials = from.UseDefaultCredentials;
        }

        public bool AllowAutoRedirect { get; set; }
        public int MaxConnectionsPerServer { get; set; }
        public bool UseCookieContainer { get; set; }
        public bool UseProxy { get; set; }
        public bool UseTracing { get; set; }
        public int? PooledConnectionLifetimeSeconds { get; set; }
        public bool UseDefaultCredentials { get; set; }
    }
}
