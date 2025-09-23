namespace Ocelot.Configuration.File;

public interface IRouteRateLimiting : IRouteUpstream, IRouteGrouping
{
    FileRateLimitByHeaderRule RateLimitOptions { get; }
}
