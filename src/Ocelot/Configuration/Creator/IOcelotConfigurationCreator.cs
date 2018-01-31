using System.Threading.Tasks;
using Ocelot.Configuration.File;
using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    public interface IOcelotConfigurationCreator
    {
        Task<Response<IOcelotConfiguration>> Create(FileConfiguration fileConfiguration);
    }
}