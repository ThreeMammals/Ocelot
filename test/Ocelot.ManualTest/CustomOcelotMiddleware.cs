using Ocelot.Logging;
using Ocelot.Middleware;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ocelot.ManualTest;

public static class CustomOcelotMiddleware
{
    public static Task Invoke(HttpContext context, Func<Task> next)
    {
        var logger = GetLogger(context);
        var downstreamRoute = context.Items.DownstreamRoute();

        if (downstreamRoute?.MetadataOptions?.Metadata is { } metadata)
        {
            logger.LogInformation(() =>
            {
                var metadataInJson = JsonSerializer.Serialize(metadata);
                var message = $"My custom middleware found some metadata: {metadataInJson}";
                return message;
            });
        }

        return next();
    }

    private static IOcelotLogger GetLogger(HttpContext context)
    {
        var loggerFactory = context.RequestServices.GetRequiredService<IOcelotLoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();
        return logger;
    }
}
