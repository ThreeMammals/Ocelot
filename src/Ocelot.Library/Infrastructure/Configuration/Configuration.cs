using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public class Configuration
    {
        public Configuration()
        {
            Routes = new List<Route>();
        }
        public Configuration(List<Route> routes)
        {
            Routes = routes;
        }

        public List<Route> Routes { get; private set; }
    }
}
