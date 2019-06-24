using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Client;
using Newtonsoft.Json.Linq;
using Ocelot.Configuration.File;
using Ocelot.Routing.ServiceFabric.Models.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ocelot.Routing.ServiceFabric
{
    internal class ServiceFabricRouteConfigurationProvider : ConfigurationProvider
    {
        private readonly IServiceFabricServicesRouteCrawler routeCrawler;

        private JObject currentConfig;

        public ServiceFabricRouteConfigurationProvider(IServiceFabricServicesRouteCrawler routeCrawler)
        {
            this.routeCrawler = routeCrawler;
        }

        public override void Load()
        {
            this.ReadAndApplyRoutesAsync(isFirstRun: true).ConfigureAwait(false).GetAwaiter().GetResult();

            // Start the backgroun sync engine.
            this.RunBackgroundWatcher();
        }

        private void RunBackgroundWatcher()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed - Automated watcher in the back.
            Task.Run(async () =>
            {
                while (true)
                {
                    await this.ReadAndApplyRoutesAsync(isFirstRun: false);

                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed - Automated watcher in the back.
        }

        private async Task ReadAndApplyRoutesAsync(bool isFirstRun = false)
        {
            IEnumerable<FileReRoute> fileRoutes = await this.GetFileReRoutesAsync().ConfigureAwait(false);

            var serializableObject = new
            {
                ReRoutes = fileRoutes,
            };

            JObject newConfig = JObject.FromObject(serializableObject);

            if (!JObject.DeepEquals(currentConfig, newConfig))
            {
                currentConfig = newConfig;
                this.Data = JsonConfigurationObjectParser.Parse(newConfig);

                if (!isFirstRun)
                {
                    this.OnReload();
                }
            }
        }

        private async Task<IEnumerable<FileReRoute>> GetFileReRoutesAsync()
        {
            IEnumerable<ServiceRouteInfo> serviceRoutes = await this.routeCrawler.GetAggregatedServiceRouteInfoAsync().ConfigureAwait(false);

            IEnumerable<FileReRoute> fileRoutes = serviceRoutes.SelectMany(serviceRoute => serviceRoute.GetOcelotRoutingConfig());

            // Establish deterministic ordering so that we can easily figure out if something changed.
            fileRoutes = fileRoutes.OrderBy(route => route.DownstreamPathTemplate + route.UpstreamPathTemplate + route.ServiceName);

            return fileRoutes;
        }
    }
}
