using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Services
{
    public interface IGetFileConfiguration
    {
        Response<FileConfiguration> Invoke();
    }
}