using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Services
{
    public interface IFileConfigurationProvider
    {
        Response<FileConfiguration> Get();
    }
}