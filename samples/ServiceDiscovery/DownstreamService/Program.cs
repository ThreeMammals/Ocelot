global using Microsoft.AspNetCore.Mvc;
using Ocelot.Samples.Web;

[assembly: ApiController]

namespace Ocelot.Samples.ServiceDiscovery.DownstreamService;

public class Program
{
    public static void Main(string[] args)
    {
        DownstreamHostBuilder.Create(args)
            .UseStartup<Startup>()
            .Build(); //.Run();
    }
}
