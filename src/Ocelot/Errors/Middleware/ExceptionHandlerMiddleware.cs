using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Errors.Middleware;

/// <summary>
/// Catches all unhandled exceptions thrown by middleware, logs and returns a 500.
/// </summary>
public class ExceptionHandlerMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRequestScopedDataRepository _repo;

    public ExceptionHandlerMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IRequestScopedDataRepository repo)
            : base(loggerFactory.CreateLogger<ExceptionHandlerMiddleware>())
    {
        _next = next;
        _repo = repo;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            httpContext.RequestAborted.ThrowIfCancellationRequested();

            var internalConfiguration = httpContext.Items.IInternalConfiguration();

            TrySetGlobalRequestId(httpContext, internalConfiguration);

            Logger.LogDebug("Ocelot pipeline started");

            await _next.Invoke(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            Logger.LogDebug("Operation canceled");
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = 499;
            }
        }
        catch (Exception e)
        {
            Logger.LogDebug("Error calling middleware");
            Logger.LogError(() => CreateMessage(httpContext, e), e);

            SetInternalServerErrorOnResponse(httpContext);
        }

        Logger.LogDebug("Ocelot pipeline finished");
    }

    private void TrySetGlobalRequestId(HttpContext httpContext, IInternalConfiguration configuration)
    {
        var key = configuration.RequestId;

        if (!string.IsNullOrEmpty(key) && httpContext.Request.Headers.TryGetValue(key, out var upstreamRequestIds))
        {
            httpContext.TraceIdentifier = upstreamRequestIds.First();
        }

        _repo.Add("RequestId", httpContext.TraceIdentifier);
    }

    private static void SetInternalServerErrorOnResponse(HttpContext httpContext)
    {
        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode = 500;
        }
    }

    private static string CreateMessage(HttpContext context, Exception e)
    {
        var original = e;
        var builder = new StringBuilder()
            .AppendLine($"{e.GetType().Name} caught in global error handler!");
        int total = 0;
        while (e.InnerException != null)
        {
            builder.AppendLine(e.InnerException.ToString());
            e = e.InnerException;
            total++;
        }

        builder.Append($"DONE reporting of a total {total} inner exception{total.Plural()} for request {context.TraceIdentifier} of the original {original.GetType().Name} below ->");
        return builder.ToString();
    }
}
