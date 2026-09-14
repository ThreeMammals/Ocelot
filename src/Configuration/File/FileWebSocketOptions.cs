namespace Ocelot.Configuration.File;

/// <summary>
/// File model for the WebSockets proxying options of the route.
/// </summary>
public class FileWebSocketOptions
{
    public FileWebSocketOptions() { }

    public FileWebSocketOptions(FileWebSocketOptions from)
    {
        BufferSize = from?.BufferSize;
    }

    public FileWebSocketOptions(WebSocketOptions from)
    {
        BufferSize = from?.BufferSize;
    }

    /// <summary>The buffer size in bytes for WebSocket proxying.</summary>
    /// <remarks>Larger values (e.g. 65536) reduce kernel transitions per second, significantly improving throughput for high-bandwidth WebSocket routes such as video streams.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value in bytes.</value>
    public int? BufferSize { get; set; }
}
