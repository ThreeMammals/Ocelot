using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public interface IOcelotConfiguration
    {
        List<ReRoute> ReRoutes { get; }
    }
}