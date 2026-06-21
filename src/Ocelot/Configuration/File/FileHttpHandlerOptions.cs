namespace Ocelot.Configuration.File;

public class FileHttpHandlerOptions
{
    public FileHttpHandlerOptions()
    { }

    public FileHttpHandlerOptions(FileHttpHandlerOptions from)
    {
        AllowAutoRedirect = from.AllowAutoRedirect;
        MaxConnectionsPerServer = from.MaxConnectionsPerServer;
        PooledConnectionLifetimeSeconds = from.PooledConnectionLifetimeSeconds;
        UseCookieContainer = from.UseCookieContainer;
        UseProxy = from.UseProxy;
        UseTracing = from.UseTracing;
        EnableMultipleHttp2Connections = from.EnableMultipleHttp2Connections;
    }

    public bool? AllowAutoRedirect { get; set; }
    public bool? EnableMultipleHttp2Connections { get; set; }
    public int? MaxConnectionsPerServer { get; set; }
    public int? PooledConnectionLifetimeSeconds { get; set; }
    public bool? UseCookieContainer { get; set; }
    public bool? UseProxy { get; set; }
    public bool? UseTracing { get; set; }
}
