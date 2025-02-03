using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot Basic setup
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot("ocelot-configuration", builder.Environment); // multiple environment files (ocelot.*.json) to be merged to ocelot.json file and write it back to disk
    //.AddOcelot("ocelot-configuration", builder.Environment, MergeOcelotJson.ToMemory); // to be merged to ocelot.json JSON-data and keep it in memory
builder.Services
    .AddOcelot(builder.Configuration);

// Add your features
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

// Add middlewares aka app.Use*()
var app = builder.Build();
await app.UseOcelot();
await app.RunAsync();
