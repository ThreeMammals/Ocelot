using Ocelot.Logging;
using Ocelot.Middleware;
using System.Text.Json;

namespace Ocelot.ManualTest.Middlewares;

public class MetadataMiddleware
{
    public static Task Invoke(HttpContext context, Func<Task> next)
    {
        var logger = GetLogger(context);
        var downstreamRoute = context.Items.DownstreamRoute();

        if (downstreamRoute?.MetadataOptions?.Metadata is { } metadata)
        {
            logger.LogInformation(() =>
            {
                var metadataInJson = JsonSerializer.Serialize(metadata, JsonSerializerOptions.Web);
                var message = $"{nameof(MetadataMiddleware)} found some metadata: {metadataInJson}";
                return message;
            });
        }

        return next();
    }

    private static IOcelotLogger GetLogger(HttpContext context)
    {
        var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
        return loggerFactory.CreateLogger<MetadataMiddleware>();
    }
}
