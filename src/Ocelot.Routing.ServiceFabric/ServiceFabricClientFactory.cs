using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Client;
using Microsoft.ServiceFabric.Common.Security;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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
                X509Certificate2 clusterCertificate = null;
                X509Store x509Store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                x509Store.Open(OpenFlags.ReadOnly);

                if (!string.IsNullOrEmpty(clientFactoryOptions.ClusterCertificateThumbprint))
                {
                    X509Certificate2Collection certCollection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, clientFactoryOptions.ClusterCertificateThumbprint, validOnly: false);

                    if (certCollection.Count == 0)
                    {
                        throw new ArgumentException("Failed to find Cluster certificate for the given thumbprint");
                    }

                    clusterCertificate = certCollection[0];
                }
                else if (!string.IsNullOrEmpty(clientFactoryOptions.ClusterCertificateSubjectName))
                {
                    string searchString = clientFactoryOptions.ClusterCertificateSubjectName;

                    if (!searchString.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                    {
                        searchString = "CN=" + searchString;
                    }

                    X509Certificate2Collection certCollection = x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, searchString, validOnly: false);

                    if (certCollection.Count == 0)
                    {
                        throw new ArgumentException("Failed to find Cluster certificate for the given subject name");
                    }

                    clusterCertificate = certCollection[0];
                }

                return SFClientLib.ServiceFabricClientFactory.Create(
                    new Uri(clientFactoryOptions.ClusterManagementEndpoint),
                    new ClientSettings(
                        () => new X509SecuritySettings(
                            clusterCertificate, 
                            new RemoteX509SecuritySettings(
                                new List<string> { clusterCertificate.Thumbprint }
                            ))));
            }
            else
            {
                return SFClientLib.ServiceFabricClientFactory.Create(new Uri(clientFactoryOptions.ClusterManagementEndpoint));
            }
        }
    }
}
