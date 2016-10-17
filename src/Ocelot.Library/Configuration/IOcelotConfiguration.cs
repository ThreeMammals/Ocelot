namespace Ocelot.Library.Configuration
{
    using System.Collections.Generic;

    public interface IOcelotConfiguration
    {
        List<ReRoute> ReRoutes { get; }
    }
}