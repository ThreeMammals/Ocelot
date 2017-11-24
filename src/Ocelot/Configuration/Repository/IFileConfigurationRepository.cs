using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Repository
{
    public interface IFileConfigurationRepository
    {
        Task<Response<FileConfiguration>> Get();
        Task<Response> Set(FileConfiguration fileConfiguration);
    }
}