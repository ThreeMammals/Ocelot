using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Newtonsoft.Json;
using Ocelot.Configuration.File;

namespace Ocelot.DependencyInjection
{
    /// <summary>
    /// Defines extension-methods for the <see cref="IConfigurationBuilder"/> interface.
    /// </summary>
    public static partial class ConfigurationBuilderExtensions
    {
        public const string PrimaryConfigFile = "ocelot.json";
        public const string GlobalConfigFile = "ocelot.global.json";
        public const string EnvironmentConfigFile = "ocelot.{0}.json";

#if NET7_0_OR_GREATER
        [GeneratedRegex(@"^ocelot\.(.*?)\.json$", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
        private static partial Regex SubConfigRegex();
#else
        private static readonly Regex SubConfigRegexVar = new(@"^ocelot\.(.*?)\.json$", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromMilliseconds(1000));
        private static Regex SubConfigRegex() => SubConfigRegexVar;
#endif

        [Obsolete("Please set BaseUrl in ocelot.json GlobalConfiguration.BaseUrl")]
        public static IConfigurationBuilder AddOcelotBaseUrl(this IConfigurationBuilder builder, string baseUrl)
        {
            var memorySource = new MemoryConfigurationSource
            {
                InitialData = new List<KeyValuePair<string, string>>
                {
                    new("BaseUrl", baseUrl),
                },
            };

            return builder.Add(memorySource);
        }

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
            var json = GetMergedOcelotJson(folder, env, null, primaryConfigFile, globalConfigFile, environmentConfigFile);
            return ApplyMergeOcelotJsonOption(builder, mergeTo, json, primaryConfigFile, optional, reloadOnChange);
        }

        private static IConfigurationBuilder ApplyMergeOcelotJsonOption(IConfigurationBuilder builder, MergeOcelotJson mergeTo, string json,
            string primaryConfigFile, bool? optional, bool? reloadOnChange)
        {
            return mergeTo == MergeOcelotJson.ToMemory ? 
                builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))) : 
                AddOcelotJsonFile(builder, json, primaryConfigFile, optional, reloadOnChange);
        }

        private static string GetMergedOcelotJson(string folder, IWebHostEnvironment env,
            FileConfiguration fileConfiguration = null, string primaryFile = null, string globalFile = null, string environmentFile = null)
        {
            var envName = string.IsNullOrEmpty(env?.EnvironmentName) ? "Development" : env.EnvironmentName;
            environmentFile ??= string.Format(EnvironmentConfigFile, envName);
            var reg = SubConfigRegex();
            var environmentFileInfo = new FileInfo(environmentFile);
            var files = new DirectoryInfo(folder)
                .EnumerateFiles()
                .Where(fi => reg.IsMatch(fi.Name) &&
                    !fi.Name.Equals(environmentFileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                    !fi.FullName.Equals(environmentFileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            fileConfiguration ??= new FileConfiguration();
            primaryFile ??= PrimaryConfigFile;
            globalFile ??= GlobalConfigFile;
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
                var config = JsonConvert.DeserializeObject<FileConfiguration>(lines);
                if (file.Name.Equals(globalFileInfo.Name, StringComparison.OrdinalIgnoreCase) &&
                    file.FullName.Equals(globalFileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    fileConfiguration.GlobalConfiguration = config.GlobalConfiguration;
                }

                fileConfiguration.Aggregates.AddRange(config.Aggregates);
                fileConfiguration.Routes.AddRange(config.Routes);
            }

            return JsonConvert.SerializeObject(fileConfiguration, Formatting.Indented);
        }

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
            var json = GetMergedOcelotJson(".", env, fileConfiguration, primaryConfigFile, globalConfigFile, environmentConfigFile);
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
        private static IConfigurationBuilder AddOcelotJsonFile(IConfigurationBuilder builder, string json,
            string primaryFile = null, bool? optional = null, bool? reloadOnChange = null) // optional injections
        {
            var primary = primaryFile ?? PrimaryConfigFile;
            File.WriteAllText(primary, json);
            return builder?.AddJsonFile(primary, optional ?? false, reloadOnChange ?? false);
        }
    }
}
