namespace Ocelot.Configuration.File;

/// <summary>
/// Provides support for creating a group of routes, instances of <see cref="IRouteGrouping"/>.
/// </summary>
public interface IRouteGroup
{
    /// <summary>The group's list of route keys (the <see cref="IRouteGrouping.Key"/> property).</summary>
    /// <value>An <see cref="IList{T}"/> collection, where <c>T</c> is a <see cref="string"/>, containing key strings.</value>
    IList<string> RouteKeys { get; set; }
}
