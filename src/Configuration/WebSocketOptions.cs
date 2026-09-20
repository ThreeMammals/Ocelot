using Ocelot.Configuration.File;

namespace Ocelot.Configuration;

public class WebSocketOptions
{
    public WebSocketOptions() { }
    public WebSocketOptions(int? bufferSize) => BufferSize = bufferSize;

    /// <summary>Initializes a new instance of the <see cref="WebSocketOptions"/> class.</summary>
    /// <remarks>This is the copying constructor.</remarks>
    /// <param name="from">The object to copy the properties from.</param>
    public WebSocketOptions(WebSocketOptions from) => BufferSize = from?.BufferSize;

    /// <summary>Initializes a new instance of the <see cref="WebSocketOptions"/> class from a <see cref="FileWebSocketOptions"/> model.</summary>
    /// <remarks>This is the converting constructor.</remarks>
    /// <param name="from">The File-model to copy the properties from.</param>
    public WebSocketOptions(FileWebSocketOptions from) => BufferSize = from?.BufferSize;

    /// <summary>Gets the buffer size in bytes for WebSocket proxying. When <see langword="null"/>, the middleware's own default is used.</summary>
    /// <remarks>Larger values (e.g. 65536) reduce kernel transitions per second, significantly improving throughput for high-bandwidth WebSocket routes such as video streams. Has no effect on non-WebSocket routes.</remarks>
    /// <value>A <see cref="Nullable{T}"/> (T is <see cref="int"/>) value in bytes, or <see langword="null"/> to use the default.</value>
    public int? BufferSize { get; init; }
}
