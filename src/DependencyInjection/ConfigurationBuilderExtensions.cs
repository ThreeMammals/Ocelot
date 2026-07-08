using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure;

namespace Ocelot.DependencyInjection;

/// <summary>
/// Defines extension-methods for the <see cref="IConfigurationBuilder"/> interface.
/// </summary>
public static partial class ConfigurationBuilderExtensions
{
    public const string PrimaryConfigFile = "ocelot.json";
    public const string GlobalConfigFile = "ocelot.global.json";
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
        // All versions of overloaded AddOcelot methods call this GetMergedOcelotJson one, so we improve Regex performance by cache increasing.
        // Developers can adjust the RegexGlobal value BEFORE calling AddOcelot
        // Developers can adjust the Regex.CacheSize value AFTER calling AddOcelot
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
        dynamic fcMerged = JObject.FromObject(fileConfiguration);

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
            dynamic fromConfig = JToken.Parse(lines);
            bool isGlobal = file.Name.Equals(globalFileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                file.FullName.Equals(globalFileInfo.FullName, StringComparison.OrdinalIgnoreCase);
            OcelotMergeConfiguration(fcMerged, fromConfig, isGlobal);
        }

        JObject jobj = (JObject)fcMerged;
        builder.Properties[MergedConfigJObjectKey] = MergedConfigJObject = jobj;
        var json = jobj.ToString();
        builder.Properties[MergedConfigJsonKey] = MergedConfigJson = json;
        return json; // returning for attachment to an IConfigurationProvider (IConfiguration), as either in-memory or file source
    }
    private static void GuardSchema(FileConfiguration configuration)
    {
        configuration.Aggregates ??= [];
        configuration.Routes ??= [];
        configuration.DynamicRoutes ??= [];
        configuration.GlobalConfiguration ??= new();
    }

    /// <summary>
    /// Adds Ocelot configuration from a ready Newtonsoft's <see cref="JObject"/> and writes the serialized JSON to the primary configuration file.
    /// </summary>
    /// <remarks>
    /// The JSON is serialized with indentation and written to the primary config file (default: <see cref="PrimaryConfigFile"/> = <c>ocelot.json</c>).<br/>
    /// Finally, the file is added as a JSON configuration provider via <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/>.<br/>
    /// Use optional arguments for custom file paths and <c>AddJsonFile</c> behavior.
    /// </remarks>
    /// <param name="builder">Configuration builder to extend.</param>
    /// <param name="fileConfiguration">The Ocelot configuration as a <see cref="JObject"/>.</param>
    /// <param name="primaryConfigFile">Primary config file path. If <see langword="null"/>, defaults to <see cref="PrimaryConfigFile"/>.</param>
    /// <param name="optional">The optional parameter for <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/>.</param>
    /// <param name="reloadOnChange">The reloadOnChange parameter for <see cref="JsonConfigurationExtensions.AddJsonFile(IConfigurationBuilder, string, bool, bool)"/>.</param>
    /// <returns>An <see cref="IConfigurationBuilder"/> object.</returns>
    public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder, JObject fileConfiguration,
        string primaryConfigFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
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
        var json = JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);
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
    /// Merges configuration sections from one Newtonsoft's <see cref="JToken"/> into another according to Ocelot's merging rules.
    /// </summary>
    /// <remarks>
    /// If <paramref name="isGlobal"/> is <see langword="true"/>, the <see cref="FileConfiguration.GlobalConfiguration"/> section is merged first.<br/>
    /// Then <c>Aggregates</c>, <c>Routes</c>, and <c>DynamicRoutes</c> sections are merged (using array merge for collections).<br/>
    /// This is an internal helper used by <see cref="GetMergedOcelotJson"/>.
    /// </remarks>
    /// <param name="to">The target <see cref="JToken"/> (usually the merged configuration).</param>
    /// <param name="from">The source <see cref="JToken"/> to merge from.</param>
    /// <param name="isGlobal">Indicates whether the source contains global configuration that should be merged into <c>GlobalConfiguration</c> section.</param>
    public static void OcelotMergeConfiguration(this JToken to, JToken from, bool isGlobal)
    {
        if (isGlobal)
            to.OcelotJMergeSection(from, nameof(FileConfiguration.GlobalConfiguration));

        to.OcelotJMergeSection(from, nameof(FileConfiguration.Aggregates))
          .OcelotJMergeSection(from, nameof(FileConfiguration.Routes))
          .OcelotJMergeSection(from, nameof(FileConfiguration.DynamicRoutes));
    }

    /// <summary>Merges a single named section from source to target Newtonsoft's <see cref="JToken"/>.</summary>
    /// <remarks>
    /// For <see cref="JObject"/> sections (e.g. <c>GlobalConfiguration</c>), the source completely replaces the destination.<br/>
    /// For <see cref="JArray"/> sections (e.g. <c>Routes</c>, <c>Aggregates</c>), items are merged using <see cref="JArray.MergeItem(object, JsonMergeSettings?)"/>.
    /// </remarks>
    /// <param name="to">The target <see cref="JToken"/>.</param>
    /// <param name="from">The source <see cref="JToken"/>.</param>
    /// <param name="sectionName">Name of the section to merge (case-insensitive).</param>
    /// <returns>The original target <see cref="JToken"/> to allow method chaining.</returns>
    public static JToken OcelotJMergeSection(this JToken to, JToken from, string sectionName)
    {
        var destination = to.OcelotJSection(sectionName);
        var source = from.OcelotJSection(sectionName);
        if (source == null || destination == null)
            return to;

        if (source is JObject)
            to.OcelotJSetSection(sectionName, source);
        else if (source is JArray)
            (destination as JArray).Merge(source);

        return to;
    }

    /// <summary>Retrieves a named section from a Newtonsoft's <see cref="JToken"/> (case-insensitive lookup).</summary>
    /// <param name="token">The <see cref="JToken"/> (typically a <see cref="JObject"/> containing Ocelot configuration).</param>
    /// <param name="name">The name of the section to retrieve (e.g. "Routes", "GlobalConfiguration").</param>
    /// <returns>The section as <see cref="JToken"/> if found; otherwise <see langword="null"/>.</returns>
    public static JToken OcelotJSection(this JToken token, string name)
    {
        JObject obj = token as JObject;
        return obj?.Properties()
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    /// <summary>Sets (or adds) a named section in the target Newtonsoft's <see cref="JToken"/>.</summary>
    /// <remarks>Performs a case-insensitive lookup for an existing property before updating or adding it.</remarks>
    /// <param name="to">The target <see cref="JToken"/> (must be a <see cref="JObject"/>).</param>
    /// <param name="name">The name of the section/property to set.</param>
    /// <param name="value">The value to assign to the section.</param>
    public static void OcelotJSetSection(this JToken to, string name, JToken value)
    {
        if (to is not JObject obj)
            return;

        var prop = obj.Properties()
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (prop != null)
            prop.Value = value;
        else
            obj.Add(name, value);
    }

    internal static readonly string MergedConfigJsonKey = typeof(ConfigurationBuilderExtensions).FullName + ".JSON";
    internal static readonly string MergedConfigJObjectKey = typeof(ConfigurationBuilderExtensions).FullName + ".JObject";
    internal static string MergedConfigJson { get; private set; }
    internal static JObject MergedConfigJObject { get; private set; }

    public static string OcelotJson(this IConfigurationBuilder builder)
        => builder.Properties[MergedConfigJsonKey] as string;
    public static JObject OcelotJObject(this IConfigurationBuilder builder)
        => builder.Properties[MergedConfigJObjectKey] as JObject;
    public static JArray OcelotJRoutes(this IConfigurationBuilder builder)
        => builder.OcelotJObject().OcelotJSection(nameof(FileConfiguration.Routes)) as JArray;
    public static JObject OcelotJGlobalConfiguration(this IConfigurationBuilder builder)
        => builder.OcelotJObject().OcelotJSection(nameof(FileConfiguration.GlobalConfiguration)) as JObject;

    public static JObject OcelotJRoute(this IConfigurationBuilder builder,
        string upstreamPathTemplate = null, string key = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        key ??= string.Empty;
        upstreamPathTemplate ??= string.Empty;
        JArray routes = builder.OcelotJRoutes() ?? [];
        JToken jt = routes
            .FirstOrDefault(r =>
                upstreamPathTemplate.Equals(r[nameof(FileRoute.UpstreamPathTemplate)]?.ToString(), comparison) ||
                key.Equals(r[nameof(FileRoute.Key)]?.ToString(), comparison));
        return jt as JObject;
    }

    public static T OcelotJGlobalProperty<T>(this IConfigurationBuilder builder, string propertyPath)
    {
        JObject global = builder.OcelotJGlobalConfiguration() ?? [];
        JToken prop = global.SelectToken(propertyPath);
        return prop is null ? default : prop.ToObject<T>();
    }

    public static T OcelotJProperty<T>(this IConfigurationBuilder builder, string propertyPath,
        string upstreamPathTemplate = null, string key = null, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        JObject route = builder.OcelotJRoute(upstreamPathTemplate, key, comparison);
        JToken prop = route?.SelectToken(propertyPath);
        return prop is not null && prop.Type != JTokenType.Null
            ? prop.ToObject<T>()
            : OcelotJGlobalProperty<T>(builder, propertyPath);
    }
}
