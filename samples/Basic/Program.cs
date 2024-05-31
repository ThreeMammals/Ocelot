using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddHealthChecks();
builder.Services.AddOcelot();
var app = builder.Build();
app.UseHealthChecks("/healthyz");
app.UseOcelot().Wait();
app.Run();