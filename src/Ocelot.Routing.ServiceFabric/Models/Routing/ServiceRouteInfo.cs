using Ocelot.Configuration.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ocelot.Routing.ServiceFabric.Models.Routing
{
    internal class ServiceRouteInfo
    {
        public IEnumerable<string> RouteTemplates { get; set; } = new List<string>();

        public string RequestTrackingHeader { get; set; }

        public string ServiceName { get; set; }

        public string DownstreamScheme { get; set; }

        public IEnumerable<FileReRoute> GetOcelotRoutingConfig()
        {
            List<FileReRoute> fileReRoutes = new List<FileReRoute>();

            foreach (string routeTemplate in this.RouteTemplates)
            {
                string[] routeTemplateParts = routeTemplate.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

                FileReRoute fileReRoute = new FileReRoute
                {
                    DownstreamPathTemplate = routeTemplateParts.Last(),
                    DownstreamScheme = this.DownstreamScheme,
                    UpstreamPathTemplate = routeTemplateParts.First(),
                    RequestIdKey = this.RequestTrackingHeader,
                    DangerousAcceptAnyServerCertificateValidator = true,
                    ServiceName = this.ServiceName,
                };

                fileReRoutes.Add(fileReRoute);
            }

            return fileReRoutes;
        }
    }
}
