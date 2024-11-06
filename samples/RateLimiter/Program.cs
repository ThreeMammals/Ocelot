using Microsoft.AspNetCore.RateLimiting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddOcelot();

builder.Services.AddRateLimiter(op =>
{
    op.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 2;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
});

var app = builder.Build();
app.UseHttpsRedirection();

await app.UseOcelot();

app.Run();
