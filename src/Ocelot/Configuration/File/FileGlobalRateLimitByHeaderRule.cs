namespace Ocelot.Configuration.File;

public class FileGlobalRateLimitByHeaderRule : FileRateLimitByHeaderRule, IRouteGroup
{
    public FileGlobalRateLimitByHeaderRule()
        : base() { }
    public FileGlobalRateLimitByHeaderRule(FileRateLimitByHeaderRule from)
        : base(from) { }

    /// <summary>Gets or sets the keys used to group routes, based on the already defined <see cref="FileRoute.Key"/> property.</summary>
    /// <remarks>If not empty, these options are applied specifically to the route with those keys; otherwise, they are applied to all routes.</remarks>
    /// <value>A <see cref="HashSet{T}"/> (where <c>T</c> is <see cref="string"/>) collection of keys that determine which routes the options should be applied to.</value>
    public HashSet<string> RouteKeys { get; set; }
}
