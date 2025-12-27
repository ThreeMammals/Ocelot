namespace Ocelot.Configuration.File;

/// <summary>
/// Allows to add this route to a group of routes as an <see cref="IRouteGroup"/> object.
/// </summary>
public interface IRouteGrouping
{
    /// <summary>The key for this route is used to group it as part of the <see cref="IRouteGroup.RouteKeys"/> collection.</summary>
    /// <value>A <see cref="string"/> object, containing key.</value>
    string Key { get; set; }
}
