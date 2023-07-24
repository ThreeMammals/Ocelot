using Ocelot.Configuration.File;
using Ocelot.Responses;
using System.Threading.Tasks;

namespace Ocelot.Configuration.Validator
{
    public interface IConfigurationValidator
    {
        Task<Response<ConfigurationValidationResult>> IsValid(FileConfiguration configuration);
    }
}
