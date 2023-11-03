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
        }

        public FileHttpHandlerOptions(FileHttpHandlerOptions from)
        {
            AllowAutoRedirect = from.AllowAutoRedirect;
            MaxConnectionsPerServer = from.MaxConnectionsPerServer;
            UseCookieContainer = from.UseCookieContainer;
            UseProxy = from.UseProxy;
        }

        public bool AllowAutoRedirect { get; set; }
        public int MaxConnectionsPerServer { get; set; }
        public bool UseCookieContainer { get; set; }
        public bool UseProxy { get; set; }
        public bool UseTracing { get; set; }
    }
}
