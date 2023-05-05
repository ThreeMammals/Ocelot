namespace Ocelot.Configuration.Validator
{
    using System.Threading.Tasks;

    using File;

    using Responses;

    public interface IConfigurationValidator
    {
        Task<Response<ConfigurationValidationResult>> IsValid(FileConfiguration configuration);
    }
}
