//var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddControllers();
//var app = builder.Build();
//app.UseHttpsRedirection();
//app.UseAuthorization();
//app.MapControllers();
//app.Run();
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot(); // single ocelot.json file in read-only mode
builder.Services
    .AddOcelot(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
//await app.UseOcelot();
var configuration = new OcelotPipelineConfiguration
{
    PreErrorResponderMiddleware = async (context, next) =>
    {
        // Do whatever you want here
        await next.Invoke(); // next is ResponderMiddleware
    },
    
};
await app.UseOcelot(configuration);
app.UseMiddleware<MetadataMiddleware>();
await app.RunAsync();
