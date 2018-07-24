using Ocelot.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.DynamicConfigurationProvider
{
    public interface IDynamicConfigurationProviderFactory
    {
        DynamicConfigurationProvider Get(IInternalConfiguration config);
    }
}
