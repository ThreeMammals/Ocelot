using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public interface IFileConfigurationRepository
    {
        Response<FileConfiguration> Get();
        Response Set(FileConfiguration fileConfiguration);
    }
}