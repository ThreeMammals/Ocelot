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

// Initialize from app configuration or hardcode/choose the best option.
bool easyWay = true;

if (easyWay)
{
    // Option #1. Define custom finder delegate to instantiate custom provider
    // by default factory which is ServiceDiscoveryProviderFactory
    builder.Services.AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute)
        => new MyServiceDiscoveryProvider(serviceProvider, config, downstreamRoute));
}
else
{
    // Option #2. Abstract from default factory (ServiceDiscoveryProviderFactory) and from FinderDelegate,
    // and build custom factory by implementation of the IServiceDiscoveryProviderFactory interface.
    builder.Services.RemoveAll<IServiceDiscoveryProviderFactory>()
        .AddSingleton<IServiceDiscoveryProviderFactory, MyServiceDiscoveryProviderFactory>();

    // Will not be called, but it is required for internal validators, aka life hack
    builder.Services.AddSingleton<ServiceDiscoveryFinderDelegate>((serviceProvider, config, downstreamRoute) => null);
}
builder.Services
    .AddOcelot(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
await app.UseOcelot();
app.Run();
