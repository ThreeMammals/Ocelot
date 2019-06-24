using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Routing.ServiceFabric
{
    internal class ServiceFabricRouteConfigurationSource : IConfigurationSource
    {
        public ServiceFabricClientFactoryOptions ClientFactoryOptions { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            if (this.ClientFactoryOptions == null)
            {
                throw new ArgumentNullException(nameof(this.ClientFactoryOptions));
            }

            ServiceFabricClientFactory clientFactory = new ServiceFabricClientFactory(Options.Create(this.ClientFactoryOptions));
            ServiceFabricServicesRouteCrawler servicesRouteCrawler = new ServiceFabricServicesRouteCrawler(clientFactory);

            return new ServiceFabricRouteConfigurationProvider(servicesRouteCrawler);
        }
    }
}
