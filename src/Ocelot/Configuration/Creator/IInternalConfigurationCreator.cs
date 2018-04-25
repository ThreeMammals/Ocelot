using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    public interface IInternalConfigurationCreator
    {
        Task<Response<IInternalConfiguration>> Create(FileConfiguration fileConfiguration);
    }
}
