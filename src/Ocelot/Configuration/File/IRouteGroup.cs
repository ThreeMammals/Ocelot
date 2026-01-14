namespace Ocelot.Configuration.File;

/// <summary>
/// Provides support for creating a group of routes, instances of <see cref="IRouteGrouping"/>.
/// </summary>
public interface IRouteGroup
{
    /// <summary>The group's list of route keys (the <see cref="IRouteGrouping.Key"/> property).</summary>
    /// <value>A <see cref="HashSet{T}"/> collection, where <c>T</c> is a <see cref="string"/>, containing key strings.</value>
    HashSet<string> RouteKeys { get; set; }
}
