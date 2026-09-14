using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator;

public class WebSocketOptionsCreator : IWebSocketOptionsCreator
{
    public WebSocketOptions Create(FileWebSocketOptions options)
        => new(options ?? new());

    public WebSocketOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.WebSocket, globalConfiguration.WebSocket);
    }

    public WebSocketOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.WebSocket, globalConfiguration.WebSocket);
    }

    protected virtual WebSocketOptions Create(IRouteGrouping grouping, FileWebSocketOptions options, FileGlobalWebSocketOptions globalOptions)
    {
        ArgumentNullException.ThrowIfNull(grouping);

        bool isGlobal = globalOptions?.RouteKeys is null // undefined section or array option -> is global
            || globalOptions.RouteKeys.Count == 0 // empty collection -> is global
            || globalOptions.RouteKeys.Contains(grouping.Key); // this route is in the group

        if (options == null && globalOptions != null && isGlobal)
        {
            return new(globalOptions);
        }

        if (options != null && globalOptions == null)
        {
            return new(options);
        }

        if (options != null && globalOptions != null)
        {
            return isGlobal ? Merge(options, globalOptions) : new(options);
        }

        return new();
    }

    protected virtual WebSocketOptions Merge(FileWebSocketOptions options, FileWebSocketOptions global)
    {
        options ??= new();
        global ??= new();
        options.BufferSize ??= global.BufferSize;
        return new(options);
    }
}
