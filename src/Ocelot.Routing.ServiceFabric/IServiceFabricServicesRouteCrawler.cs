using System.Collections.Generic;
using System.Threading.Tasks;
using Ocelot.Routing.ServiceFabric.Models.Routing;

namespace Ocelot.Routing.ServiceFabric
{
    /// <summary>
    /// Service fabric services route crawler.
    /// </summary>
    internal interface IServiceFabricServicesRouteCrawler
    {
        /// <summary>
        /// Gets the collection <see cref="ServiceRouteInfo"/> representing all the services running on cluster which support Ocelot routing requests to them.
        /// </summary>
        /// <returns>Collection of <see cref="ServiceRouteInfo"/></returns>
        Task<IEnumerable<ServiceRouteInfo>> GetAggregatedServiceRouteInfoAsync();
    }
}
