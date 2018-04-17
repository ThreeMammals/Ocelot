using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public interface IInternalConfiguration
    {
        List<ReRoute> ReRoutes { get; }
        string AdministrationPath {get;}
        ServiceProviderConfiguration ServiceProviderConfiguration {get;}
        string RequestId {get;}
    }
}
