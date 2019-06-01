using Ocelot.Middleware;

namespace Ocelot.Cache
{
    public interface ICacheKeyGenerator
    {
        string GenerateRequestCacheKey(DownstreamContext context);
    }
}
