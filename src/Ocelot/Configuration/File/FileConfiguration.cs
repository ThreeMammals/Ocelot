using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileConfiguration
    {
        public FileConfiguration()
        {
            ReRoutes = new List<FileReRoute>();
            GlobalConfiguration = new FileGlobalConfiguration();
            AuthenticationOptions = new List<FileAuthenticationOptions>();
        }

        public List<FileReRoute> ReRoutes { get; set; }
        public FileGlobalConfiguration GlobalConfiguration { get; set; }
        public List<FileAuthenticationOptions> AuthenticationOptions { get; set; }
    }
}
