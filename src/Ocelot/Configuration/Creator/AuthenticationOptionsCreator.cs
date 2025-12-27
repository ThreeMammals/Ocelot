using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.Configuration.Creator;

public class AuthenticationOptionsCreator : IAuthenticationOptionsCreator
{
    public AuthenticationOptions Create(FileAuthenticationOptions options)
        => new(options);

    public AuthenticationOptions Create(FileRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.AuthenticationOptions, globalConfiguration.AuthenticationOptions);
    }

    public AuthenticationOptions Create(FileDynamicRoute route, FileGlobalConfiguration globalConfiguration)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(globalConfiguration);
        return Create(route, route.AuthenticationOptions, globalConfiguration.AuthenticationOptions);
    }

    protected virtual AuthenticationOptions Create(IRouteGrouping grouping, FileAuthenticationOptions options, FileGlobalAuthenticationOptions globalOptions)
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

    protected virtual AuthenticationOptions Merge(FileAuthenticationOptions options, FileAuthenticationOptions globalOptions)
    {
        options.AllowAnonymous ??= globalOptions.AllowAnonymous;
        options.AllowedScopes ??= globalOptions.AllowedScopes;
        options.AuthenticationProviderKey = options.AuthenticationProviderKey.IfEmpty(globalOptions.AuthenticationProviderKey);
        options.AuthenticationProviderKeys ??= globalOptions.AuthenticationProviderKeys;
        return new(options);
    }
}
