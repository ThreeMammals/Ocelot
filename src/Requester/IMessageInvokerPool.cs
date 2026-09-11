using Ocelot.Configuration;

namespace Ocelot.Requester;

/// <summary>
/// A pool implementation for <see cref="HttpMessageInvoker"/> pooling.
/// <para>
/// Largely inspired by StackExchange implementation.
/// Link: <see href="https://github.com/StackExchange/StackExchange.Utils/blob/main/src/StackExchange.Utils.Http/DefaultHttpClientPool.cs">StackExchange.Utils.DefaultHttpClientPool</see>.
/// </para>
/// </summary>
public interface IMessageInvokerPool
{
    /// <summary>
    /// Gets a client for the specified <see cref="DownstreamRoute"/>.
    /// </summary>
    /// <param name="downstreamRoute">The route to get a Message Invoker for.</param>
    /// <returns>A <see cref="HttpMessageInvoker"/> from the pool.</returns>
    HttpMessageInvoker Get(DownstreamRoute downstreamRoute);

    /// <summary>
    /// Clears the pool, in case you need to.
    /// </summary>
    void Clear();
}
