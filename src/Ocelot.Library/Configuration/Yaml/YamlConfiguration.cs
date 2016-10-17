namespace Ocelot.Library.Configuration.Yaml
{
    using System.Collections.Generic;

    public class YamlConfiguration
    {
        public YamlConfiguration()
        {
            ReRoutes = new List<YamlReRoute>();
        }

        public List<YamlReRoute> ReRoutes { get; set; }
    }
}
