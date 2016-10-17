namespace Ocelot.Library.Configuration.Yaml
{
    using Responses;

    public interface IConfigurationValidator
    {
        Response<ConfigurationValidationResult> IsValid(YamlConfiguration configuration);
    }
}
