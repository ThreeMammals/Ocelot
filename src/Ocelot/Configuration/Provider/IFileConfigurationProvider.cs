using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Provider
{
    public interface IFileConfigurationProvider
    {
        Response<FileConfiguration> Get();
    }
}