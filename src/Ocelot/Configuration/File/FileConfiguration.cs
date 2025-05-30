﻿namespace Ocelot.Configuration.File;

public class FileConfiguration
{
    public FileConfiguration()
    {
        Routes = new();
        DynamicRoutes = new();
        Aggregates = new();
        GlobalConfiguration = new();
    }

    public List<FileRoute> Routes { get; set; }
    public List<FileDynamicRoute> DynamicRoutes { get; set; }

    // Seperate field for aggregates because this let's you re-use Routes in multiple Aggregates
    public List<FileAggregateRoute> Aggregates { get; set; }

    public FileGlobalConfiguration GlobalConfiguration { get; set; }
}
