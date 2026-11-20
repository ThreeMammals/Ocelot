using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.DownstreamPathManipulation;

public class ClaimsToDownstreamPathMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IChangeDownstreamPathTemplate _changeDownstreamPathTemplate;

    public ClaimsToDownstreamPathMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IChangeDownstreamPathTemplate changeDownstreamPathTemplate)
        : base(loggerFactory.CreateLogger<ClaimsToDownstreamPathMiddleware>())
    {
        _next = next;
        _changeDownstreamPathTemplate = changeDownstreamPathTemplate;
    }

    public async Task Invoke(HttpContext context)
    {
        var downstreamRoute = context.Items.DownstreamRoute();
        if (downstreamRoute.ClaimsToPath.Count != 0)
        {
            Logger.LogInformation(() => $"{downstreamRoute.DownstreamPathTemplate.Value} has instructions to convert claims to path");

            var templatePlaceholderNameAndValues = context.Items.TemplatePlaceholderNameAndValues();

            var response = _changeDownstreamPathTemplate.ChangeDownstreamPath(downstreamRoute.ClaimsToPath, context.User.Claims,
                downstreamRoute.DownstreamPathTemplate, templatePlaceholderNameAndValues);

            if (response.IsError)
            {
                Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                context.Items.UpsertErrors(response.Errors);
                return;
            }
        }

        await _next.Invoke(context);
    }
}
