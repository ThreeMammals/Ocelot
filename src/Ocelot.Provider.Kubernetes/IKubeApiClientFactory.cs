using KubeClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Provider.Kubernetes
{
    public interface IKubeApiClientFactory
    {
        IKubeApiClient Get(KubeRegistryConfiguration config);
    }
}
