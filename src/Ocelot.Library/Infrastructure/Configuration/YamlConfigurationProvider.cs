using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.Library.Infrastructure.Configuration
{
    using System.IO;
    using Microsoft.Extensions.Configuration;

    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        public YamlConfigurationProvider(YamlConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            var parser = new YamlConfigurationFileParser();

            Data = parser.Parse(stream);
        }
    }
}
