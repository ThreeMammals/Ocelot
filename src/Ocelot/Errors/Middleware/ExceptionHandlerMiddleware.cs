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

    public async Task Invoke(HttpContext context)
    {
        try
        {
            context.RequestAborted.ThrowIfCancellationRequested();

            var configuration = context.Items.IInternalConfiguration();
            TrySetGlobalRequestId(context, configuration);

            Logger.LogDebug("Ocelot pipeline started");
            await _next.Invoke(context);
        }
        catch (OperationCanceledException e) when (context.RequestAborted.IsCancellationRequested)
        {
            Logger.LogDebug("Operation canceled");
            Logger.LogWarning(() => CreateMessage(context, e));
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 499; // custom Ocelot code
            }
        }
        catch (Exception e)
        {
            Logger.LogDebug("Error calling middleware");
            Logger.LogError(() => CreateMessage(context, e), e);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        finally
        {
            Logger.LogDebug("Ocelot pipeline finished");
        }
    }

    private void TrySetGlobalRequestId(HttpContext context, IInternalConfiguration configuration)
    {
        var key = configuration.RequestId;
        if (!string.IsNullOrEmpty(key) && context.Request.Headers.TryGetValue(key, out var upstreamRequestIds))
        {
            context.TraceIdentifier = upstreamRequestIds.First();
        }

        _repo.Add(nameof(IInternalConfiguration.RequestId), context.TraceIdentifier);
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
