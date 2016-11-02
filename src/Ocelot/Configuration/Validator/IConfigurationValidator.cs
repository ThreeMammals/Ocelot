using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Validator
{
    public interface IConfigurationValidator
    {
        Response<ConfigurationValidationResult> IsValid(FileConfiguration configuration);
    }
}
