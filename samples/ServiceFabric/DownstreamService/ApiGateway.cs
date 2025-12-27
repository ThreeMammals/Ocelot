using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Ocelot.Samples.Web;
using System.Fabric;

namespace Ocelot.Samples.ServiceFabric.DownstreamService;

/// <summary>
/// The FabricRuntime creates an instance of this class for each service type instance. 
/// </summary>
internal sealed class ApiGateway : StatelessService
{
    public ApiGateway(StatelessServiceContext context)
        : base(context) { }

    /// <summary>
    /// Optional override to create listeners (like tcp, http) for this service instance.
    /// </summary>
    /// <returns>The collection of listeners.</returns>
    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        return new ServiceInstanceListener[]
        {
            new(serviceContext =>
                new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                {
                    Console.WriteLine($"Starting Kestrel on {url}");
                    ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                    _ = OcelotHostBuilder.Create();
                    var builder = WebApplication.CreateBuilder(); //(args);
                    builder.Services
                        .AddSingleton(serviceContext)
                        .AddControllers();
                    builder.WebHost
                    //.UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                        .UseUrls(url);
                    if (builder.Environment.IsDevelopment())
                    {
                        builder.Logging.AddConsole();
                    }
                    var app = builder.Build();
                    app.MapControllers();
                    if (app.Environment.IsDevelopment())
                    {
                        app.UseDeveloperExceptionPage();
                    }
                    return app;
                }))
        };
    }
}
