namespace Ocelot.Configuration.File;

public interface IRouteRateLimiting : IRouteGrouping
{
    FileRateLimitByHeaderRule RateLimitOptions { get; }
}
