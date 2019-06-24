using System.Threading.Tasks;
using Microsoft.ServiceFabric.Client;

namespace Ocelot.Routing.ServiceFabric
{
    /// <summary>
    /// Factory to get instance(s) of <see cref="IServiceFabricClient"/>.
    /// </summary>
    internal interface IServiceFabricClientFactory
    {
        /// <summary>
        /// Gets a client to access service fabric client APIs.
        /// </summary>
        /// <returns>Service fabric client.</returns>
        Task<IServiceFabricClient> GetServiceFabricClientAsync();
    }
}
