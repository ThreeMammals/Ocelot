using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;
using Ocelot.Provider.Polly;
using Ocelot.Samples.Web;

//_ = OcelotHostBuilder.Create(args);
var builder = WebApplication.CreateBuilder(args);

// Ocelot Basic setup
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddOcelot(builder.Configuration)
    .AddEureka()
    .AddPolly();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
await app.UseOcelot();
app.Run();
