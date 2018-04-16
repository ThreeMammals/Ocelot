using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Ocelot.DependencyInjection
{
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
            var memorySource = new MemoryConfigurationSource();
            memorySource.InitialData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("BaseUrl", baseUrl)
            };
            builder.Add(memorySource);
            return builder;
        }

        public static IConfigurationBuilder AddOcelot(this IConfigurationBuilder builder)
        {
            const string pattern = "(?i)ocelot(.*).json$";

            var reg = new Regex(pattern);

            var files = Directory.GetFiles(".")
                .Where(path => reg.IsMatch(path)).Where(x => x.Count(s => s == '.') == 3)
                .ToList();

            FileConfiguration ocelotConfig = new FileConfiguration();

            foreach (var file in files)
            {
                if(files.Count > 1 && file == "./ocelot.json")
                {
                    continue;
                }

                var lines = File.ReadAllText(file);
                
                var config = JsonConvert.DeserializeObject<FileConfiguration>(lines);

                if(file ==  "./ocelot.global.json")
                {
                    ocelotConfig.GlobalConfiguration = config.GlobalConfiguration;
                }

                ocelotConfig.Aggregates.AddRange(config.Aggregates);
                ocelotConfig.ReRoutes.AddRange(config.ReRoutes);
            }

            var json = JsonConvert.SerializeObject(ocelotConfig);

            File.WriteAllText("ocelot.json", json);

            builder.AddJsonFile("ocelot.json");

            return builder;
        }
    }
}
