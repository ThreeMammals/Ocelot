using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Ocelot.DependencyInjection
{
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
            //var load all files with ocelot*.json
            //merge these files into one
            //save it as ocelot.json
            builder.AddJsonFile("ocelot.json");
            return builder;
        }
    }
}
