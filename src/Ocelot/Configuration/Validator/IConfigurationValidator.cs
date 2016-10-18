using Ocelot.Configuration.Yaml;
using Ocelot.Responses;

namespace Ocelot.Configuration.Validator
{
    public interface IConfigurationValidator
    {
        Response<ConfigurationValidationResult> IsValid(YamlConfiguration configuration);
    }
}
