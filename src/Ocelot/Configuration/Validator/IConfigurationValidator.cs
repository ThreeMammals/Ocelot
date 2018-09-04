namespace Ocelot.Configuration.Validator
{
    using System.Threading.Tasks;
    using Ocelot.Configuration.File;
    using Ocelot.Responses;

    public interface IConfigurationValidator
    {
        Task<Response<ConfigurationValidationResult>> IsValid(FileConfiguration configuration);
    }
}
