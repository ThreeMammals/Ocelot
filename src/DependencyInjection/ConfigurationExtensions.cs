using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;

namespace Ocelot.DependencyInjection;

/// <summary>
/// Extensions of the <see cref="IConfiguration"/> interface.
/// </summary>
public static class ConfigurationExtensions
{
    public static IConfigurationRoot OcelotRoot(this IConfiguration configuration)
        => configuration as IConfigurationRoot; // in future we could attach any section, for example "Ocelot"
    public static IConfigurationSection OcelotRoutes(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.Routes));
    public static IConfigurationSection OcelotDynamicRoutes(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.DynamicRoutes));
    public static IConfigurationSection OcelotAggregates(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.Aggregates));
    public static IConfigurationSection OcelotGlobalConfiguration(this IConfiguration configuration)
        => configuration.GetSection(nameof(FileConfiguration.GlobalConfiguration));

    public static IConfigurationSection OcelotRoute(this IConfiguration configuration,
        string upstreamPathTemplate = null, string key = null, StringComparison comparison = StringComparison.Ordinal)
    {
        key ??= string.Empty;
        upstreamPathTemplate ??= string.Empty;
        var routes = OcelotRoutes(configuration);
        return routes.GetChildren().FirstOrDefault(r =>
            upstreamPathTemplate.Equals(r[nameof(FileRoute.UpstreamPathTemplate)], comparison) ||
            key.Equals(r[nameof(FileRoute.Key)], comparison));
    }

    public static IConfigurationSection OcelotDynamicRoute(this IConfiguration configuration,
        string serviceName = null, string serviceNamespace = null, string key = null, StringComparison comparison = StringComparison.Ordinal)
    {
        key ??= string.Empty;
        serviceName ??= string.Empty;
        serviceNamespace ??= string.Empty;
        var routes = OcelotDynamicRoutes(configuration);
        return routes.GetChildren().FirstOrDefault(r =>
            (serviceNamespace.IsEmpty() && serviceName.Equals(r[nameof(FileDynamicRoute.ServiceName)], comparison)) ||
            (serviceNamespace.IsNotEmpty() && serviceName.Equals(r[nameof(FileDynamicRoute.ServiceName)], comparison) && serviceNamespace.Equals(r[nameof(FileDynamicRoute.ServiceNamespace)], comparison)) ||
            key.Equals(r[nameof(FileDynamicRoute.Key)], comparison));
    }

    public static IConfigurationSection OcelotAggregate(this IConfiguration configuration, string upstreamPathTemplate, StringComparison comparison = StringComparison.Ordinal)
    {
        upstreamPathTemplate ??= string.Empty;
        var routes = OcelotAggregates(configuration);
        return routes.GetChildren().FirstOrDefault(r =>
            upstreamPathTemplate.Equals(r[nameof(FileAggregateRoute.UpstreamPathTemplate)], comparison));
    }

    public static IConfigurationSection OcelotSection(this IConfiguration configuration, string sectionName, IConfigurationSection current = null)
    {
        current ??= OcelotRoutes(configuration);
        var property = current.GetChildren().FirstOrDefault(p => p.Key == sectionName);
        property ??= OcelotGlobalSection(configuration, sectionName);
        return property;
    }
    public static T OcelotProperty<T>(this IConfiguration configuration, string propertyName, IConfigurationSection current = null)
    {
        current ??= OcelotRoutes(configuration);
        return OcelotSection(configuration, propertyName, current).Get<T>();
    }

    public static IConfigurationSection OcelotGlobalSection(this IConfiguration configuration, string sectionName)
    {
        var global = OcelotGlobalConfiguration(configuration);
        return global.GetChildren().FirstOrDefault(p => p.Key == sectionName);
    }
    public static T OcelotGlobalProperty<T>(this IConfiguration configuration, string propertyName)
        => OcelotGlobalSection(configuration, propertyName).Get<T>();
}
