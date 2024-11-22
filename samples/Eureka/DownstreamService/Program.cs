using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Samples.Web;
using System;

namespace Ocelot.Samples.Eureka.DownstreamService;

public class Program
{
    public static void Main(string[] args)
    {
        DownstreamHostBuilder.Create(args)
            .UseUrls($"http://{Environment.MachineName}:5001")
            .UseStartup<Startup>()
            .Build()
            .Run();
    }

}
