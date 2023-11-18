global using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore;

[assembly: ApiController]

namespace Ocelot.Samples.ServiceDiscovery.DownstreamService;

public class Program
{
    public static void Main(string[] args)
    {
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build()
            .Run();
    }
}
