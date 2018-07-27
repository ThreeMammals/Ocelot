using System;
using System.Collections.Generic;
using System.Text;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.File;

namespace Ocelot.Configuration.Creator
{
    public class DynamicConfigurationCreator : IDynamicConfigurationCreator
    {
        public DynamicReRouteConfiguration Create(FileDynamicReRouteConfiguration dynamicReRouteConfiguration)
        {
            return new DynamicConfigurationBuilder()
                .WithStore(dynamicReRouteConfiguration.Store)
                .WithServer(dynamicReRouteConfiguration.Host, dynamicReRouteConfiguration.Port)
                .Build();
        }
    }
}
