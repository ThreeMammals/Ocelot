using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileConfiguration
    {
        public FileConfiguration()
        {
            Routes = new List<FileRoute>();
            GlobalConfiguration = new FileGlobalConfiguration();
            Aggregates = new List<FileAggregateRoute>();
            DynamicRoutes = new List<FileDynamicRoute>();
            Clusters = new Dictionary<string, FileCluster>();
        }

        public List<FileRoute> Routes { get; set; }

        public Dictionary<string, FileCluster> Clusters { get; set; }

        public List<FileDynamicRoute> DynamicRoutes { get; set; }

        // Seperate field for aggregates because this let's you re-use Routes in multiple Aggregates
        public List<FileAggregateRoute> Aggregates { get; set; }

        public FileGlobalConfiguration GlobalConfiguration { get; set; }
    }

    public class FileCluster
    { 
        public FileCluster()
        {
            Destinations = new Dictionary<string, FileDestination>();
        }

        public FileLoadBalancing LoadBalancing { get; set; }
        public FileHttpClient HttpClient { get; set; }
        public Dictionary<string, FileDestination> Destinations { get; set; }
    }

    public class FileLoadBalancing
    {
        public string Mode { get; set; }
    }

    public class FileHttpClient
    {
        public List<string> SslProtocols { get; set; }
        public int MaxConnectionsPerServer { get; set; }
        public bool DangerousAcceptAnyServerCertificate { get; set; }
    }

    public class FileDestination
    {
        public string Address { get; set; }
    }
}
