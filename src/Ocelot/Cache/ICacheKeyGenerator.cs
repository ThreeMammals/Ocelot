using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public interface ICacheKeyGenerator
    {
        ValueTask<string> GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute);
    }
}
