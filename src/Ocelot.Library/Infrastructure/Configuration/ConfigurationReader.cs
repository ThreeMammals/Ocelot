namespace Ocelot.Library.Infrastructure.Configuration
{
    using System.IO;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class ConfigurationReader : IConfigurationReader
    {
        public Configuration Read(string configurationFilePath)
        {
            var contents = File.ReadAllText(configurationFilePath);

            var input = new StringReader(contents);

            var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());

            var configuration = deserializer.Deserialize<Configuration>(input);

            return configuration;;
        }
    }
}