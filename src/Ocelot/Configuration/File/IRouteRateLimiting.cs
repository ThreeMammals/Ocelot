namespace Ocelot.Configuration.File;

public interface IRouteRateLimiting : IRouteUpstream
{
    FileRateLimitRule RateLimitOptions { get; }
}
