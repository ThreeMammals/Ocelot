using Ocelot.Library.Infrastructure.Responses;

namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public interface IConfigurationValidator
    {
        Response<ConfigurationValidationResult> IsValid(YamlConfiguration configuration);
    }
}
