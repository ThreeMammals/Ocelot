namespace Ocelot.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.Memory;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Configuration.File;
    using Newtonsoft.Json;

    public static class ConfigurationBuilderExtensions
    {
        [Obsolete("Please set BaseUrl in ocelot.json GlobalConfiguration.BaseUrl")]
        public static IConfigurationBuilder AddOcelotBaseUrl(this IConfigurationBuilder builder, string baseUrl)
        {
            var memorySource = new MemoryConfigurationSource
            {
                InitialData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("BaseUrl", baseUrl)
                }
            };

            builder.Add(memorySource);

            return builder;
        }

        public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder)
        {
            const string pattern = "(?i)ocelot.([a-zA-Z0-9]*).json";

            var reg = new Regex(pattern);

            var files = Directory.GetFiles(".")
                .Where(path => reg.IsMatch(path))
                .ToList();

            var fileConfiguration = new FileConfiguration();

            foreach (var file in files)
            {
                // windows and unix sigh...
                if(files.Count > 1 && (file == "./ocelot.json" || file == ".\\ocelot.json"))
                {
                    continue;
                }

                var lines = File.ReadAllText(file);
                
                var config = JsonConvert.DeserializeObject<FileConfiguration>(lines);

                // windows and unix sigh...
                if (file == "./ocelot.global.json" || file == ".\\ocelot.global.json")
                {
                    fileConfiguration.GlobalConfiguration = config.GlobalConfiguration;
                }

                fileConfiguration.Aggregates.AddRange(config.Aggregates);
                fileConfiguration.ReRoutes.AddRange(config.ReRoutes);
            }

            var json = JsonConvert.SerializeObject(fileConfiguration);

            File.WriteAllText("ocelot.json", json);

            builder.AddJsonFile("ocelot.json");

            return builder;
        }
    }
}
