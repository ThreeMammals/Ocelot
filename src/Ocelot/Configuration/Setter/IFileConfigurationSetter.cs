using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Setter
{
    public interface IFileConfigurationSetter
    {
        Task<Response> Set(FileConfiguration config);
    }
}