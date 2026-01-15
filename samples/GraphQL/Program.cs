using GraphQL.Types;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Samples.GraphQL;
using Ocelot.Samples.GraphQL.Models;
using Ocelot.Samples.Web;

var schema = Schema.For(@"
        type Hero {
            id: Int
            name: String
        }

        type Query {
            hero(id: Int): Hero
        }
    ", _ =>
{
    _.Types.Include<Query>();
});

//_ = OcelotHostBuilder.Create(args);
var builder = WebApplication.CreateBuilder(args);

// Ocelot Basic setup
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot();
builder.Services
    .AddSingleton<ISchema>(schema)
    .AddOcelot(builder.Configuration)
    .AddDelegatingHandler<GraphQLDelegatingHandler>();

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}

var app = builder.Build();
await app.UseOcelot();
app.Run();
