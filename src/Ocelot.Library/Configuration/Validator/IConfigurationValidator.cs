using Ocelot.Library.Configuration.Yaml;
using Ocelot.Library.Responses;

namespace Ocelot.Library.Configuration.Validator
{
    public interface IConfigurationValidator
    {
        Response<ConfigurationValidationResult> IsValid(YamlConfiguration configuration);
    }
}
