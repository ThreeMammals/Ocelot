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
                .Where(path => reg.IsMatch(path))
                .ToList();

            foreach (var file in files)
            {
                var lines = File.ReadAllText(file);
                var config = JsonConvert.DeserializeObject<FileConfiguration>(lines);

            }

            //var load all files with ocelot*.json
            //merge these files into one
            //save it as ocelot.json
            builder.AddJsonFile("ocelot.json");
            return builder;
        }
    }
}
