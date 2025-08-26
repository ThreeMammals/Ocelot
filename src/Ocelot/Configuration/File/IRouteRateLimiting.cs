namespace Ocelot.Configuration.File;

public interface IRouteRateLimiting : IRouteUpstream
{
    FileRateLimitByHeaderRule RateLimitOptions { get; }
}
