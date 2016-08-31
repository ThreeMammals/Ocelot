namespace Ocelot.Library.Infrastructure.Configuration
{
    using Microsoft.Extensions.Configuration;

    public class YamlConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            FileProvider = FileProvider ?? builder.GetFileProvider();
            return new YamlConfigurationProvider(this);
        }
    }
}
