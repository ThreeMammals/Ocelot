using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Configuration
{
    public class Configuration
    {
        public Configuration()
        {
            ReRoutes = new List<ReRoute>();
        }

        public List<ReRoute> ReRoutes { get; set; }
    }
}
