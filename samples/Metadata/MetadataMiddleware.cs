using Microsoft.Extensions.Logging;
using Ocelot.Logging;
using Ocelot.Metadata;
using Ocelot.Middleware;
using System.Text.Json;

namespace Ocelot.Samples.Metadata;

public class MetadataMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;

    public MetadataMiddleware(RequestDelegate next, IOcelotLoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<MetadataMiddleware>())
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        Logger.LogDebug("My middleware started");
        var route = context.Items.DownstreamRoute();
        var id = route.GetMetadata<string>("id");
        var tags = route.GetMetadata<string[]>("tags");

        // Plugin 1 data
        var p1Enabled = route.GetMetadata<bool>("plugin1.enabled");
        var p1Values = route.GetMetadata<string[]>("plugin1.values");
        var p1Param = route.GetMetadata<string>("plugin1.param", "system-default-value");
        var p1Param2 = route.GetMetadata<int>("plugin1.param2");

        // Plugin 2 data
        var p2Param1 = route.GetMetadata<string>("plugin2/param1", "default-value");
        var json = route.GetMetadata<string>("plugin2/data", "{}");
        var plugin2 = JsonSerializer.Deserialize<Plugin2Data>(json);

        // Reading global metadata
        var globalInstanceName = route.GetMetadata<string>("instance_name");
        var globalPlugin2Param1 = route.GetMetadata<string>("plugin2/param1");

        // Working with plugin's metadata
        // ...
        return _next.Invoke(context);
    }
    public class Plugin2Data
    {
        public string? name { get; set; }
        public int? age { get; set; }
        public string? city { get; set; }
        public bool? is_student { get; set; }
        public string[]? hobbies { get; set; }
    }
}
