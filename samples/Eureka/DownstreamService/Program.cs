using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Ocelot.Samples.Web;
using System;

namespace DownstreamService;

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
