using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SFClientLib = Microsoft.ServiceFabric.Client;

namespace Ocelot.Routing.ServiceFabric
{
    /// <summary>
    /// Service fabric client factory.
    /// </summary>
    internal class ServiceFabricClientFactory : IServiceFabricClientFactory
    {
        private IServiceFabricClient serviceFabricClient;

        private ServiceFabricClientFactoryOptions clientFactoryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFabricClientFactory"/> class.
        /// </summary>
        /// <param name="clientFactoryOptions">Client factory options to create Service Fabric client.</param>
        public ServiceFabricClientFactory(IOptions<ServiceFabricClientFactoryOptions> clientFactoryOptions)
        {
            this.clientFactoryOptions = clientFactoryOptions.Value;
        }

        /// <summary>
        /// Gets a service fabric client.
        /// </summary>
        /// <returns>Service fabric client.</returns>
        public Task<IServiceFabricClient> GetServiceFabricClientAsync()
        {
            return this.GetServiceFabricClientInternalAsync();
        }

        private async Task<IServiceFabricClient> GetServiceFabricClientInternalAsync(bool forceCreateNewConnection = false)
        {
            if (serviceFabricClient == null || forceCreateNewConnection)
            {
                IServiceFabricClient sfClient = ServiceFabricClientFactory.CreateServiceFabricClient(this.clientFactoryOptions);
                this.serviceFabricClient = sfClient;
                return sfClient;
            }

            try
            {
                // A random call to check if the connection is still ok. If this throws it means the connection is in a bad state
                // and we create a new connection. Otherwise we keep using the same connection.
                await this.serviceFabricClient.Cluster.GetClusterHealthAsync().ConfigureAwait(false);
                return this.serviceFabricClient;
            }
            catch
            {
                return await this.GetServiceFabricClientInternalAsync(forceCreateNewConnection: true).ConfigureAwait(false);
            }
        }

        private static IServiceFabricClient CreateServiceFabricClient(ServiceFabricClientFactoryOptions clientFactoryOptions)
        {
            if (clientFactoryOptions.IsSecuredCluster)
            {
                ////return SFClientLib.ServiceFabricClientFactory.Create(
                ////    new Uri(clientFactoryOptions.ClusterManagementEndpoint),
                ////    new ClientSettings
                ////    {
                ////        SecuritySettings = () =>
                ////        {
                ////            return Microsoft.ServiceFabric.Common.Security.X509SecuritySettings
                ////            {

                ////            }
                ////        }
                ////    });
                return null;
            }
            else
            {
                return SFClientLib.ServiceFabricClientFactory.Create(new Uri(clientFactoryOptions.ClusterManagementEndpoint));
            }
        }
    }
}
