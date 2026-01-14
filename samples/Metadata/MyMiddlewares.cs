using Ocelot.Logging;
using Ocelot.Metadata;
using Ocelot.Middleware;
using Ocelot.Responder;
using System.Text.Json;

namespace Ocelot.Samples.Metadata;
using Models;

class MyMiddlewares
{
    public static async Task PreErrorResponderMiddleware(HttpContext context, Func<Task> next)
    {
        // Get downstream response first
        await next.Invoke(); // ResponderMiddleware

        var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
        var logger = loggerFactory.CreateLogger<MyMiddlewares>();
        logger.LogDebug(() => $"My custom {nameof(PreErrorResponderMiddleware)} started");

        var route = context.Items.DownstreamRoute();
        var routeId = route.GetMetadata<int>("route.id");
        var routeName = route.GetMetadata("route.name", string.Empty);
        switch (routeId)
        {
            case 1: // ocelot-docs
            case 2: // ocelot-docs-BFF
                bool disabled = route.GetMetadata<bool>("disableMetadataJson");
                break;
            case 3: // weather-current
                var cities = route.GetMetadata<string[]>("cities");
                var defaultCity = route.GetMetadata<string>("cities.default");
                var citiesUS = route.GetMetadata<string[]>("cities.US");
                var pathTemperatureCelsius = route.GetMetadata<string>("temperature-celsius-path");
                var dataResponse = route.GetMetadata<WeatherResponse>("data/Response", new());
                // TODO Refactor Ocelot Metadata helpers and Ocelot Core to support propagation of the JsonElement and JsonNode
                //var dataLocation = route.GetMetadataElement("stub-data/location", new JsonElement());
                break;
            case 4: // ocelot-posts
                var id = route.GetMetadata<string>("id");
                var tags = route.GetMetadata<string[]>("tags");

                // Plugin 1 data
                var p1Enabled = route.GetMetadata<bool>("plugin1.enabled");
                var p1Values = route.GetMetadata<string[]>("plugin1.values");
                var p1Param = route.GetMetadata("plugin1.param", "system-default-value");
                var p1Param2 = route.GetMetadata<int>("plugin1.param2");

                // Plugin 2 data
                var p2Param1 = route.GetMetadata("plugin2/param1", "default-value");
                var plugin2 = route.GetMetadata<PostsPlugin2>("plugin2/data", new());
                break;
            case 5: // test-deflate
                var response1 = route.GetMetadata<TestDeflateResponse>("data/Response", new());
                break;
            case 6: // test-gzip
                var json = route.GetMetadata("data/Response", "{}"); // parse data manually
                var response2 = JsonSerializer.Deserialize<TestGZipResponse>(json);
                break;
        }
        // Reading global metadata
        var globalAppName = route.GetMetadata<string>("app-name");
        // Working with metadata
        // ...
    }

    public static async Task ResponderMiddleware(HttpContext context, Func<Task> next)
    {
        // Prepare services
        var responder = context.RequestServices.GetRequiredService<IHttpResponder>();
        var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
        var logger = loggerFactory.CreateLogger<MyMiddlewares>();
        var codeMapper = context.RequestServices.GetRequiredService<IErrorsToHttpStatusCodeMapper>();
        logger.LogDebug(() => $"My custom {nameof(ResponderMiddleware)} started");

        // Call original middleware
        Task @delegate(HttpContext c) => next();
        var @base = new Responder.Middleware.ResponderMiddleware(@delegate, responder, loggerFactory, codeMapper);
        await @base.Invoke(context); // next.Invoke()
    }
}
