using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public interface IOcelotConfiguration
    {
        List<ReRoute> ReRoutes { get; }
        string AdministrationPath {get;}
        ServiceProviderConfiguration ServiceProviderConfiguration {get;}
    }
}