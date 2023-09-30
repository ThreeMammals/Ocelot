using Ocelot.Configuration;
using Ocelot.Request.Middleware;

namespace Ocelot.Cache
{
    public interface ICacheKeyGenerator
    {
        string GenerateRequestCacheKey(DownstreamRequest downstreamRequest, DownstreamRoute downstreamRoute);
    }
}
