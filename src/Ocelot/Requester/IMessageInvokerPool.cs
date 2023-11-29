using Ocelot.Configuration;

namespace Ocelot.Requester;

/// <summary>
/// Largely inspired by stack exchange implementation
/// https://github.com/StackExchange/StackExchange.Utils/blob/main/src/StackExchange.Utils.Http/DefaultHttpClientPool.cs.
/// </summary>
public interface IMessageInvokerPool
{
    HttpMessageInvoker Get(DownstreamRoute downstreamRoute);
    void Clear();
}
