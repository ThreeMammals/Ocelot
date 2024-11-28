using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Ocelot.Samples.Web;

namespace Ocelot.Samples.Kubernetes.ApiGateway;

public class Program
{
    public static void Main(string[] args)
    {
        OcelotHostBuilder.Create(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", true, true)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                    .AddJsonFile("ocelot.json", false, false)
                    .AddEnvironmentVariables();
            })
        .UseStartup<Startup>()
        .Build()
        .Run();
    }
}
