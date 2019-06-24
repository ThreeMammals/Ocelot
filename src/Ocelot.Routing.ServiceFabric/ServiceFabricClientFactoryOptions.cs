using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Routing.ServiceFabric
{
    public class ServiceFabricClientFactoryOptions
    {
        public string ClusterManagementEndpoint { get; set; }

        public bool IsSecuredCluster { get; set; }

        public string ClusterCertificateThumbprint { get; set; }

        public string ClusterCertificateSubjectName { get; set; }
    }
}
