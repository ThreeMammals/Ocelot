using KubeClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Provider.Kubernetes
{
    public class KubeRegistryConfiguration
    {
        public Uri ApiEndPoint { get; set; }

        public string KubeNamespace { get; set; }

        public string KeyOfServiceInK8s { get; set; }

        public KubeAuthStrategy AuthStrategy { get; set; }

        public string AccessToken { get; set; }

        public bool AllowInsecure { get; set; }
    }
}
