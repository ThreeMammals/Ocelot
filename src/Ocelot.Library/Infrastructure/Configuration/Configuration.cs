using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public class Configuration
    {
        public Configuration()
        {
            Routes = new List<Route>();
        }

        public List<Route> Routes { get; set; }
    }
}
