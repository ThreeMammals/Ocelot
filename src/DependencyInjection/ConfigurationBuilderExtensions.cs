using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;
using Ocelot.Infrastructure.Extensions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ocelot.DependencyInjection;

/// <summary>
/// Defines extension-methods for the <see cref="IConfigurationBuilder"/> interface.
/// </summary>
public static partial class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Gets the default name of the primary Ocelot configuration file.
    /// </summary>
    public const string PrimaryConfigFile = "ocelot.json";

    /// <summary>
    /// Gets the default name of the global Ocelot configuration file.
    /// </summary>
    public const string GlobalConfigFile = "ocelot.global.json";

    /// <summary>
    /// Gets the default pattern for environment-specific Ocelot configuration files.
    /// </summary>
    public const string EnvironmentConfigFile = "ocelot.{0}.json";

    /// <summary>
    /// Adds Ocelot configuration by environment, reading the required files from the default path.
    /// </summary>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="env">Web hosting environment object.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, IWebHostEnvironment env)
        => builder.AddOcelot(".", env);

    /// <summary>
    /// Adds Ocelot configuration by environment, reading the required files from the specified folder.
    /// </summary>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="folder">Folder to read files from.</param>
    /// <param name="env">Web hosting environment object.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, string folder, IWebHostEnvironment env)
        => builder.AddOcelot(folder, env, MergeOcelotJson.ToFile);

    /// <summary>
    /// Adds Ocelot configuration by environment and merge option, reading the required files from the current default folder.
    /// </summary>
    /// <remarks>Use optional arguments for injections and overridings.</remarks>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="env">Web hosting environment object.</param>
    /// <param name="mergeTo">Option to merge files to.</param>
    /// <param name="primaryConfigFile">Primary config file.</param>
    /// <param name="globalConfigFile">Global config file.</param>
    /// <param name="environmentConfigFile">Environment config file.</param>
    /// <param name="optional">The 2nd argument of the AddJsonFile.</param>
    /// <param name="reloadOnChange">The 3rd argument of the AddJsonFile.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, IWebHostEnvironment env, MergeOcelotJson mergeTo,
        string primaryConfigFile = null, string globalConfigFile = null, string environmentConfigFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
        => builder.AddOcelot(".", env, mergeTo, primaryConfigFile, globalConfigFile, environmentConfigFile, optional, reloadOnChange);

    /// <summary>
    /// Adds Ocelot configuration by environment and merge option, reading the required files from the specified folder.
    /// </summary>
    /// <remarks>Use optional arguments for injections and overridings.</remarks>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="folder">Folder to read files from.</param>
    /// <param name="env">Web hosting environment object.</param>
    /// <param name="mergeTo">Option to merge files to.</param>
    /// <param name="primaryConfigFile">Primary config file.</param>
    /// <param name="globalConfigFile">Global config file.</param>
    /// <param name="environmentConfigFile">Environment config file.</param>
    /// <param name="optional">The 2nd argument of the AddJsonFile.</param>
    /// <param name="reloadOnChange">The 3rd argument of the AddJsonFile.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, string folder, IWebHostEnvironment env, MergeOcelotJson mergeTo,
        string primaryConfigFile = null, string globalConfigFile = null, string environmentConfigFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
    {
        var json = GetMergedOcelotJson(builder, folder, env, null, primaryConfigFile, globalConfigFile, environmentConfigFile);
        primaryConfigFile ??= Path.Join(folder, PrimaryConfigFile); // if not specified, merge & write back to the same folder
        return ApplyMergeOcelotJsonOption(builder, mergeTo, json, primaryConfigFile, optional, reloadOnChange);
    }

    private static IConfigurationBuilder ApplyMergeOcelotJsonOption(IConfigurationBuilder builder, MergeOcelotJson mergeTo, string json,
        string primaryConfigFile, bool? optional, bool? reloadOnChange)
    {
        return mergeTo == MergeOcelotJson.ToMemory ?
            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))) :
            AddOcelotJsonFile(builder, json, primaryConfigFile, optional, reloadOnChange);
    }

    [GeneratedRegex(@"^ocelot\.(.*?)\.json$", RegexOptions.IgnoreCase | RegexOptions.Singleline, RegexGlobal.DefaultMatchTimeoutMilliseconds, "en-US")]
    private static partial Regex SubConfigRegex();

    /// <summary>
    /// Merges multiple Ocelot configuration files (primary, global, environment-specific, and additional sub-configs) into a single JSON string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the core merging method used by all <c>AddOcelot</c> overloads. It discovers files matching the pattern 
    /// <c>ocelot.*.json</c> (case-insensitive), excluding the environment-specific file, and merges them according to 
    /// Ocelot's configuration merging rules (via <see cref="OcelotMergeConfiguration"/>).
    /// </para>
    /// <para>Supports splitting configuration across files for better maintainability (e.g., routes in separate files, global settings, environment overrides).</para>
    /// <para>Regex cache size is optimized for performance (see <see cref="RegexGlobal.RegexCacheSize"/>).</para>
    /// </remarks>
    /// <param name="builder">Configuration builder.</param>
    /// <param name="folder">The root folder where configuration files are located.</param>
    /// <param name="env">The web hosting environment used to determine the environment-specific config file name.</param>
    /// <param name="fileConfiguration">Optional base <see cref="FileConfiguration"/> to start merging from.
    /// If <see langword="null"/>, a new empty instance is used.</param>
    /// <param name="primaryFile">Optional full path to the primary configuration file (defaults to <c>ocelot.json</c> in the given folder).
    /// Used to skip re-processing the primary file when multiple files exist.</param>
    /// <param name="globalFile">Optional full path to the global configuration file (defaults to <c>ocelot.global.json</c> in the given folder).</param>
    /// <param name="environmentFile">Optional full path to the environment-specific configuration file (defaults to <c>ocelot.{EnvironmentName}.json</c>).</param>
    /// <returns>A JSON <see cref="string"/> containing the fully merged Ocelot configuration (ready to be written to file or added as a memory source).</returns>
    public static string GetMergedOcelotJson(this IConfigurationBuilder builder, string folder, IWebHostEnvironment env,
        FileConfiguration fileConfiguration = null, string primaryFile = null, string globalFile = null, string environmentFile = null)
    {
        Regex.CacheSize = RegexGlobal.RegexCacheSize;

        var envName = string.IsNullOrEmpty(env?.EnvironmentName) ? "Development" : env.EnvironmentName;
        environmentFile ??= Path.Join(folder, string.Format(EnvironmentConfigFile, envName));
        var reg = SubConfigRegex();
        var environmentFileInfo = new FileInfo(environmentFile);
        var files = new DirectoryInfo(folder)
            .EnumerateFiles()
            .Where(fi => reg.IsMatch(fi.Name) &&
                !fi.Name.Equals(environmentFileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                !fi.FullName.Equals(environmentFileInfo.FullName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        fileConfiguration ??= new FileConfiguration();
        GuardSchema(fileConfiguration);

        // Convert FileConfiguration to JsonObject
        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonString = JsonSerializer.Serialize(fileConfiguration, options);
        var fcMerged = JsonNode.Parse(jsonString)?.AsObject() ?? [];

        primaryFile ??= Path.Join(folder, PrimaryConfigFile);
        globalFile ??= Path.Join(folder, GlobalConfigFile);
        var primaryFileInfo = new FileInfo(primaryFile);
        var globalFileInfo = new FileInfo(globalFile);
        foreach (var file in files)
        {
            if (files.Length > 1 &&
                file.Name.Equals(primaryFileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                file.FullName.Equals(primaryFileInfo.FullName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var lines = File.ReadAllText(file.FullName);
            var fromConfig = JsonNode.Parse(lines, null, SkipComments); // clean Json without comments
            bool isGlobal = file.Name.Equals(globalFileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                file.FullName.Equals(globalFileInfo.FullName, StringComparison.OrdinalIgnoreCase);
            OcelotMergeConfiguration(fcMerged, fromConfig, isGlobal);
        }

        var jobj = fcMerged;
        builder.Properties[MergedConfigJsonKey] = MergedConfigJson = jobj.ToJsonString(options);
        builder.Properties[MergedConfigJObjectKey] = MergedConfigJObject = jobj;
        return builder.Properties[MergedConfigJsonKey] as string;
    }

    private static void GuardSchema(FileConfiguration configuration)
    {
        configuration.Aggregates ??= [];
        configuration.Routes ??= [];
        configuration.DynamicRoutes ??= [];
        configuration.GlobalConfiguration ??= new();
    }

    public static readonly JsonDocumentOptions SkipComments = new()
    {
        CommentHandling = JsonCommentHandling.Skip,  // Skip comments
        AllowTrailingCommas = true                   // Allow trailing commas
    };

    /// <summary>
    /// Adds Ocelot configuration from a ready System.Text.Json <see cref="JsonObject"/> and writes the serialized JSON to the primary configuration file.
    /// </summary>
    /// <remarks>
    /// The JSON is serialized with indentation and written to the primary config file (default: <see cref="PrimaryConfigFile"/> = <c>ocelot.json</c>).<br/>
    /// Finally, the file is added as a JSON configuration provider via <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/>.<br/>
    /// Use optional arguments for custom file paths and <c>AddJsonFile</c> behavior.
    /// </remarks>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="fileConfiguration">The Ocelot configuration as a <see cref="JsonObject"/>.</param>
    /// <param name="primaryConfigFile">Primary config file path. If <see langword="null"/>, defaults to <see cref="PrimaryConfigFile"/>.</param>
    /// <param name="optional">The optional parameter for <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/>.</param>
    /// <param name="reloadOnChange">The reloadOnChange parameter for <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/>.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, JsonObject fileConfiguration,
        string primaryConfigFile = null, bool? optional = null, bool? reloadOnChange = null)
        => SerializeToFile(builder, fileConfiguration, primaryConfigFile, optional, reloadOnChange);

    /// <summary>
    /// Adds Ocelot configuration by ready configuration object and writes JSON to the primary configuration file.<br/>
    /// Finally, adds JSON file as configuration provider.
    /// </summary>
    /// <remarks>Use optional arguments for injections and overridings.</remarks>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="fileConfiguration">File configuration to add as JSON provider.</param>
    /// <param name="primaryConfigFile">Primary config file.</param>
    /// <param name="optional">The 2nd argument of the AddJsonFile.</param>
    /// <param name="reloadOnChange">The 3rd argument of the AddJsonFile.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, FileConfiguration fileConfiguration,
        string primaryConfigFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
        => SerializeToFile(builder, fileConfiguration, primaryConfigFile, optional, reloadOnChange);

    private static IConfigurationBuilder SerializeToFile(IConfigurationBuilder builder, object fileConfiguration,
        string primaryConfigFile = null, bool? optional = null, bool? reloadOnChange = null)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(fileConfiguration, options);
        return AddOcelotJsonFile(builder, json, primaryConfigFile, optional, reloadOnChange);
    }

    /// <summary>
    /// Adds Ocelot configuration by ready configuration object, environment and merge option, reading the required files from the current default folder.
    /// </summary>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="fileConfiguration">File configuration to add as JSON provider.</param>
    /// <param name="env">Web hosting environment object.</param>
    /// <param name="mergeTo">Option to merge files to.</param>
    /// <param name="primaryConfigFile">Primary config file.</param>
    /// <param name="globalConfigFile">Global config file.</param>
    /// <param name="environmentConfigFile">Environment config file.</param>
    /// <param name="optional">The 2nd argument of the AddJsonFile.</param>
    /// <param name="reloadOnChange">The 3rd argument of the AddJsonFile.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, FileConfiguration fileConfiguration, IWebHostEnvironment env, MergeOcelotJson mergeTo,
        string primaryConfigFile = null, string globalConfigFile = null, string environmentConfigFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
    {
        var json = GetMergedOcelotJson(builder, ".", env, fileConfiguration, primaryConfigFile, globalConfigFile, environmentConfigFile);
        return ApplyMergeOcelotJsonOption(builder, mergeTo, json, primaryConfigFile, optional, reloadOnChange);
    }

    /// <summary>
    /// Adds Ocelot primary configuration file (aka ocelot.json).<br/>
    /// Writes JSON to the file.<br/>
    /// Adds the file as a JSON configuration provider via the <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/> extension.
    /// </summary>
    /// <remarks>Use optional arguments for injections and overridings.</remarks>
    /// <param name="builder">The builder to extend.</param>
    /// <param name="json">JSON data of the Ocelot configuration.</param>
    /// <param name="primaryFile">Primary config file.</param>
    /// <param name="optional">The 2nd argument of the AddJsonFile.</param>
    /// <param name="reloadOnChange">The 3rd argument of the AddJsonFile.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelotJsonFile(this IConfigurationBuilder builder, string json,
        string primaryFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
    {
        var primary = primaryFile ?? PrimaryConfigFile;
        File.WriteAllText(primary, json);
        return builder.AddJsonFile(primary, optional ?? false, reloadOnChange ?? false);
    }

    /// <summary>
    /// Adds Ocelot primary configuration file (aka ocelot.json) in read-only mode.
    /// <para>Adds the file as a JSON configuration provider via the <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/> extension.</para>
    /// </summary>
    /// <remarks>Use optional arguments for injections and overridings.</remarks>
    /// <param name="builder">The builder to extend.</param>
    /// <param name="primaryFile">Primary config file path.</param>
    /// <param name="optional">The 2nd argument of the AddJsonFile.</param>
    /// <param name="reloadOnChange">The 3rd argument of the AddJsonFile.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder,
        string primaryFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
        => builder.AddJsonFile(primaryFile ?? PrimaryConfigFile, optional ?? false, reloadOnChange ?? false);

    /// <summary>
    /// Merges configuration sections from one <c>System.Text.Json</c> <see cref="JsonNode"/> into another according to Ocelot's merging rules.
    /// </summary>
    /// <remarks>
    /// If <paramref name="isGlobal"/> is <see langword="true"/>, the <see cref="FileConfiguration.GlobalConfiguration"/> section is merged first.<br/>
    /// Then <c>Aggregates</c>, <c>Routes</c>, and <c>DynamicRoutes</c> sections are merged (using array merge for collections).<br/>
    /// This is an internal helper used by <see cref="GetMergedOcelotJson"/>.
    /// </remarks>
    /// <param name="to">The target <see cref="JsonNode"/> (usually the merged configuration).</param>
    /// <param name="from">The source <see cref="JsonNode"/> to merge from.</param>
    /// <param name="isGlobal">Indicates whether the source contains global configuration that should be merged into <c>GlobalConfiguration</c> section.</param>
    public static void OcelotMergeConfiguration(this JsonNode to, JsonNode from, bool isGlobal)
    {
        if (isGlobal)
            to.OcelotJMergeSection(from, nameof(FileConfiguration.GlobalConfiguration));

        to.OcelotJMergeSection(from, nameof(FileConfiguration.Aggregates))
          .OcelotJMergeSection(from, nameof(FileConfiguration.Routes))
          .OcelotJMergeSection(from, nameof(FileConfiguration.DynamicRoutes));
    }

    /// <summary>Merges a single named section from source to target System.Text.Json <see cref="JsonNode"/>.</summary>
    /// <remarks>
    /// For <see cref="JsonObject"/> sections (e.g. <c>GlobalConfiguration</c>), the source completely replaces the destination.<br/>
    /// For <see cref="JsonArray"/> sections (e.g. <c>Routes</c>, <c>Aggregates</c>), items are merged using custom array merge logic.
    /// </remarks>
    /// <param name="to">The target <see cref="JsonNode"/>.</param>
    /// <param name="from">The source <see cref="JsonNode"/>.</param>
    /// <param name="sectionName">Name of the section to merge (case-insensitive).</param>
    /// <returns>The original target <see cref="JsonNode"/> to allow method chaining.</returns>
    public static JsonNode OcelotJMergeSection(this JsonNode to, JsonNode from, string sectionName)
    {
        var destination = to.OcelotJSection(sectionName);
        var source = from.OcelotJSection(sectionName);
        if (source == null || destination == null)
            return to;

        if (source is JsonObject)
            to.OcelotJSetSection(sectionName, source);
        else if (source is JsonArray sourceArray && destination is JsonArray destArray)
            OcelotJMergeArray(destArray, sourceArray);

        return to;
    }

    /// <summary>
    /// Merges two JsonArrays by adding all items from the source array to the destination array.
    /// </summary>
    /// <param name="destination">The destination array.</param>
    /// <param name="source">The source array to merge from.</param>
    private static void OcelotJMergeArray(JsonArray destination, JsonArray source)
    {
        foreach (var item in source)
        {
            if (item != null)
                destination.Add(item.DeepClone());
        }
    }

    /// <summary>Retrieves a named section from a System.Text.Json <see cref="JsonNode"/> (case-insensitive lookup).</summary>
    /// <param name="node">The <see cref="JsonNode"/> (typically a <see cref="JsonObject"/> containing Ocelot configuration).</param>
    /// <param name="name">The name of the section to retrieve (e.g. "Routes", "GlobalConfiguration").</param>
    /// <returns>The section as <see cref="JsonNode"/> if found; otherwise <see langword="null"/>.</returns>
    public static JsonNode OcelotJSection(this JsonNode node, string name)
    {
        if (node is not JsonObject obj)
            return null;

        var property = obj.FirstOrDefault(p =>
            p.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

        return property.Value;
    }

    /// <summary>Sets (or adds) a named section in the target System.Text.Json <see cref="JsonNode"/>.</summary>
    /// <remarks>Performs a case-insensitive lookup for an existing property before updating or adding it.</remarks>
    /// <param name="to">The target <see cref="JsonNode"/> (must be a <see cref="JsonObject"/>).</param>
    /// <param name="name">The name of the section/property to set.</param>
    /// <param name="value">The value to assign to the section.</param>
    public static void OcelotJSetSection(this JsonNode to, string name, JsonNode value)
    {
        if (to is not JsonObject obj)
            return;

        // Find existing property (case-insensitive)
        var existingKey = obj.FirstOrDefault(p =>
            p.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Key;

        if (existingKey != null)
            obj[existingKey] = value?.DeepClone();
        else
            obj[name] = value?.DeepClone();
    }

    internal static readonly string MergedConfigJsonKey = typeof(ConfigurationBuilderExtensions).FullName + ".JSON";
    internal static readonly string MergedConfigJObjectKey = typeof(ConfigurationBuilderExtensions).FullName + ".JObject";

    /// <summary>
    /// Gets the merged Ocelot JSON payload cached on the configuration builder.
    /// </summary>
    internal static string MergedConfigJson { get; private set; }

    /// <summary>
    /// Gets the merged Ocelot JSON object cached on the configuration builder.
    /// </summary>
    internal static JsonObject MergedConfigJObject { get; private set; }

    /// <summary>
    /// Gets the merged Ocelot JSON payload previously cached on the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <returns>The merged JSON payload as a string, or <see langword="null"/> if no merged payload has been cached.</returns>
    public static string OcelotJson(this IConfigurationBuilder builder)
        => builder.Properties[MergedConfigJsonKey] as string;

    /// <summary>
    /// Gets the merged Ocelot JSON object previously cached on the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <returns>The merged JSON object, or <see langword="null"/> if no merged payload has been cached.</returns>
    public static JsonObject OcelotJObject(this IConfigurationBuilder builder)
        => builder.Properties[MergedConfigJObjectKey] as JsonObject;

    /// <summary>
    /// Gets the routes section from the merged Ocelot configuration.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <returns>The routes section as a <see cref="JsonArray"/>, or <see langword="null"/> if it is not present.</returns>
    public static JsonArray OcelotJRoutes(this IConfigurationBuilder builder)
        => builder.OcelotJObject()?.OcelotJSection(nameof(FileConfiguration.Routes)) as JsonArray;

    /// <summary>
    /// Gets the dynamic routes section from the merged Ocelot configuration.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <returns>The dynamic routes section as a <see cref="JsonArray"/>, or <see langword="null"/> if it is not present.</returns>
    public static JsonArray OcelotJDynamicRoutes(this IConfigurationBuilder builder)
        => builder.OcelotJObject()?.OcelotJSection(nameof(FileConfiguration.DynamicRoutes)) as JsonArray;

    /// <summary>
    /// Gets the aggregates section from the merged Ocelot configuration.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <returns>The aggregates section as a <see cref="JsonArray"/>, or <see langword="null"/> if it is not present.</returns>
    public static JsonArray OcelotJAggregates(this IConfigurationBuilder builder)
        => builder.OcelotJObject()?.OcelotJSection(nameof(FileConfiguration.Aggregates)) as JsonArray;

    /// <summary>
    /// Gets the global configuration section from the merged Ocelot configuration.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <returns>The global configuration section as a <see cref="JsonObject"/>, or <see langword="null"/> if it is not present.</returns>
    public static JsonObject OcelotJGlobalConfiguration(this IConfigurationBuilder builder)
        => builder.OcelotJObject()?.OcelotJSection(nameof(FileConfiguration.GlobalConfiguration)) as JsonObject;

    /// <summary>
    /// Finds the first route object matching the specified upstream path template or key.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <param name="upstreamPathTemplate">The upstream path template to match.</param>
    /// <param name="key">The route key to match.</param>
    /// <param name="comparison">The string comparison mode used when evaluating the route values.</param>
    /// <returns>The matching route as a <see cref="JsonObject"/>, or <see langword="null"/> if no route matches.</returns>
    public static JsonObject OcelotJRoute(this IConfigurationBuilder builder,
        string upstreamPathTemplate = null, string key = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        key ??= string.Empty;
        upstreamPathTemplate ??= string.Empty;
        JsonArray routes = builder.OcelotJRoutes() ?? [];

        foreach (var route in routes)
        {
            if (route is JsonObject routeObj)
            {
                var routeKey = routeObj[nameof(FileRoute.Key)]?.GetValue<string>();
                var template = routeObj[nameof(FileRoute.UpstreamPathTemplate)]?.GetValue<string>();

                if (upstreamPathTemplate.Equals(template, comparison) || key.Equals(routeKey, comparison))
                    return routeObj;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the first dynamic route object matching the specified service name, namespace, or key.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <param name="serviceName">The service name to match.</param>
    /// <param name="serviceNamespace">The service namespace to match.</param>
    /// <param name="key">The dynamic route key to match.</param>
    /// <param name="comparison">The string comparison mode used when evaluating the route values.</param>
    /// <returns>The matching dynamic route as a <see cref="JsonObject"/>, or <see langword="null"/> if no dynamic route matches.</returns>
    public static JsonObject OcelotJDynamicRoute(this IConfigurationBuilder builder,
        string serviceName = null, string serviceNamespace = null, string key = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        key ??= string.Empty;
        serviceName ??= string.Empty;
        serviceNamespace ??= string.Empty;
        JsonArray routes = builder.OcelotJDynamicRoutes() ?? [];

        foreach (var route in routes)
        {
            if (route is JsonObject routeObj)
            {
                var routeKey = routeObj[nameof(FileDynamicRoute.Key)]?.GetValue<string>();
                var name = routeObj[nameof(FileDynamicRoute.ServiceName)]?.GetValue<string>();
                var ns = routeObj[nameof(FileDynamicRoute.ServiceNamespace)]?.GetValue<string>();

                if ((serviceNamespace.IsEmpty() && serviceName.Equals(name, comparison)) ||
                    (serviceNamespace.IsNotEmpty() && serviceName.Equals(name, comparison) && serviceNamespace.Equals(ns, comparison)) ||
                    key.Equals(routeKey, comparison))
                {
                    return routeObj;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the first aggregate route object matching the specified upstream path template.
    /// </summary>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <param name="upstreamPathTemplate">The upstream path template to match.</param>
    /// <param name="comparison">The string comparison mode used when evaluating the aggregate route values.</param>
    /// <returns>The matching aggregate route as a <see cref="JsonObject"/>, or <see langword="null"/> if no aggregate route matches.</returns>
    public static JsonObject OcelotJAggregate(this IConfigurationBuilder builder, string upstreamPathTemplate, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        upstreamPathTemplate ??= string.Empty;
        JsonArray routes = builder.OcelotJAggregates() ?? [];

        foreach (var route in routes)
        {
            if (route is JsonObject routeObj)
            {
                var template = routeObj[nameof(FileAggregateRoute.UpstreamPathTemplate)]?.GetValue<string>();
                if (upstreamPathTemplate.Equals(template, comparison))
                    return routeObj;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a strongly typed value from the global configuration section using a JSONPath expression.
    /// </summary>
    /// <typeparam name="T">The type of the value to deserialize.</typeparam>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <param name="propertyPath">The JSONPath expression used to locate the property.</param>
    /// <returns>The property value if found; otherwise, the <see langword="default"/> value for <typeparamref name="T"/>.</returns>
    public static T OcelotJGlobalProperty<T>(this IConfigurationBuilder builder, string propertyPath)
    {
        JsonObject global = builder.OcelotJGlobalConfiguration() ?? [];
        // System.Text.Json doesn't have built-in JSONPath support, so we need to implement or use a library
        // For simplicity, we'll use a helper method to navigate the path
        var prop = NavigateJsonPath(global, propertyPath);
        return prop is null ? default : prop.Deserialize<T>();
    }

    /// <summary>
    /// Gets a strongly typed value from the matching route, falling back to the global configuration section when necessary.
    /// </summary>
    /// <typeparam name="T">The type of the value to deserialize.</typeparam>
    /// <param name="builder">The configuration builder that stores the merged Ocelot configuration.</param>
    /// <param name="propertyPath">The JSONPath expression used to locate the property.</param>
    /// <param name="upstreamPathTemplate">The upstream path template used to locate the route.</param>
    /// <param name="key">The route key used to locate the route.</param>
    /// <param name="comparison">The string comparison mode used when evaluating the route values.</param>
    /// <returns>The property value if found; otherwise, the <see langword="default"/> value for <typeparamref name="T"/>.</returns>
    public static T OcelotJProperty<T>(this IConfigurationBuilder builder, string propertyPath,
        string upstreamPathTemplate = null, string key = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        var route = builder.OcelotJRoute(upstreamPathTemplate, key, comparison);
        var prop = route != null ? NavigateJsonPath(route, propertyPath) : null;
        return prop is not null && prop.GetValueKind() != JsonValueKind.Null
            ? prop.Deserialize<T>()
            : OcelotJGlobalProperty<T>(builder, propertyPath);
    }

    /// <summary>
    /// Gets a strongly typed value from the specified route or from the supplied global configuration section.
    /// </summary>
    /// <typeparam name="T">The type of the value to deserialize.</typeparam>
    /// <param name="route">The route object to inspect.</param>
    /// <param name="propertyPath">The JSONPath expression used to locate the property.</param>
    /// <param name="globalConfiguration">The optional global configuration section to use as a fallback.</param>
    /// <returns>The property value if found; otherwise, the <see langword="default"/> value for <typeparamref name="T"/>.</returns>
    public static T OcelotJProperty<T>(this JsonObject route, string propertyPath, JsonObject globalConfiguration = null)
    {
        globalConfiguration ??= MergedConfigJObject?.OcelotJSection(nameof(FileConfiguration.GlobalConfiguration)) as JsonObject;
        var prop = route != null ? NavigateJsonPath(route, propertyPath) : null;
        var glob = globalConfiguration != null ? NavigateJsonPath(globalConfiguration, propertyPath) : null;

        if (prop is not null && prop.GetValueKind() != JsonValueKind.Null)
            return prop.Deserialize<T>();

        return glob is not null && glob.GetValueKind() != JsonValueKind.Null
            ? glob.Deserialize<T>()
            : default;
    }

    /// <summary>
    /// Helper method to navigate a JSON path (simple dot notation) within a JsonNode.
    /// </summary>
    /// <param name="node">The starting JsonNode.</param>
    /// <param name="path">The path to navigate (e.g., "LoadBalancerOptions.Type").</param>
    /// <returns>The JsonNode at the specified path, or null if not found.</returns>
    private static JsonNode NavigateJsonPath(JsonNode node, string path)
    {
        if (string.IsNullOrEmpty(path) || node == null)
            return node;

        var segments = path.Split('.');
        var current = node;

        foreach (var segment in segments)
        {
            if (current is JsonObject obj)
            {
                if (obj.TryGetPropertyValue(segment, out var next))
                    current = next;
                else
                    return null;
            }
            else
            {
                return null;
            }
        }

        return current;
    }
}
