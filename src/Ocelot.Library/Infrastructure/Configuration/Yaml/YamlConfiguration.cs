using System.Collections.Generic;

namespace Ocelot.Library.Infrastructure.Configuration.Yaml
{
    public class YamlConfiguration
    {
        public YamlConfiguration()
        {
            ReRoutes = new List<YamlReRoute>();
        }

        public List<YamlReRoute> ReRoutes { get; set; }
    }
}
