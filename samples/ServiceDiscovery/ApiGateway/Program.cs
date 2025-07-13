using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples.ServiceDiscovery.ApiGateway.ServiceDiscovery;
using Ocelot.Samples.Web;
using Ocelot.ServiceDiscovery;

_ = OcelotHostBuilder.Create(args);
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();

// Perform initialization from application configuration or hardcode/choose the best option.
bool easyWay = true;

if (easyWay)
{
    // Design #1: Define a custom finder delegate to instantiate a custom provider 
    // under the default factory (ServiceDiscoveryProviderFactory).
    builder.Services
        .AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute)
            => new MyServiceDiscoveryProvider(serviceProvider, config, downstreamRoute));
}
else
{
    // Design #2: Abstract from the default factory (ServiceDiscoveryProviderFactory) and FinderDelegate,
    // and create your own factory by implementing the IServiceDiscoveryProviderFactory interface.
    builder.Services
        .RemoveAll<IServiceDiscoveryProviderFactory>()
        .AddSingleton<IServiceDiscoveryProviderFactory, MyServiceDiscoveryProviderFactory>();

    // This will not be called but is required for internal validators. It's also a handy workaround.
    builder.Services
        .AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute) => null);
}

builder.Services
    .AddOcelot(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
await app.UseOcelot();
await app.RunAsync();
