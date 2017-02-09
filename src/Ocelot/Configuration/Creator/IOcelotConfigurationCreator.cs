using System.Threading.Tasks;
using Ocelot.Responses;

namespace Ocelot.Configuration.Creator
{
    public interface IOcelotConfigurationCreator
    {
        Task<Response<IOcelotConfiguration>> Create();
    }
}