using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Globalization;

namespace Ocelot.RateLimiting;

public class RateLimitingMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimiting _limiter;
    private readonly IHttpContextAccessor _contextAccessor;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOcelotLoggerFactory factory,
        IRateLimiting limiter,
        IHttpContextAccessor contextAccessor)
        : base(factory.CreateLogger<RateLimitingMiddleware>())
    {
        _next = next;
        _limiter = limiter;
        _contextAccessor = contextAccessor;
    }

    public Task Invoke(HttpContext context)
    {
        var now = DateTime.UtcNow;
        context.Items.Add(nameof(DateTime.UtcNow), now);
        var downstreamRoute = context.Items.DownstreamRoute();
        var options = downstreamRoute.RateLimitOptions ?? new(false);
        if (!options.EnableRateLimiting)
        {
            Logger.LogInformation(() => $"Rate limiting is disabled for route '{downstreamRoute.Name()}' via the {nameof(RateLimitOptions.EnableRateLimiting)} option.");
            return _next.Invoke(context);
        }

        var identity = Identify(context, options, downstreamRoute);
        if (IsWhitelisted(identity, options))
        {
            Logger.LogInformation(() => $"Route '{downstreamRoute.Name()}' is configured to bypass rate limiting based on the client's header, due to the client's ID being detected in the whitelist.");
            return _next.Invoke(context);
        }

        var rule = options.Rule;
        if (rule.Limit <= 0) // TODO: Move to File-model validator(s)
        {
            var message = $"Rate limiting is misconfigured for the route '{downstreamRoute.Name()}' due to an invalid rule -> {rule}!";
            Logger.LogWarning(message);
            RateLimitOptions errorOpts = new(options)
            {
                QuotaMessage = message,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return BreakExecution(context, errorOpts, 999_999_999.999D);
        }

        var counter = _limiter.ProcessRequest(identity, options, now);
        if (counter.Total > rule.Limit)
        {
            var retryAfter = _limiter.RetryAfter(counter, rule, now); // compute retry after value based on counter state
            LogBlockedRequest(context, identity, counter, rule, downstreamRoute); // log blocked request virtually
            return BreakExecution(context, options, retryAfter);
        }

        // Set X-RateLimit-* headers for the longest period
        var originalContext = _contextAccessor.HttpContext;
        if (options.EnableHeaders && originalContext != null)
        {
            var headers = _limiter.GetHeaders(originalContext, identity, options, now, counter);
            originalContext.Response.OnStarting(SetRateLimitHeaders, state: headers);
            Logger.LogInformation(() => $"Route '{downstreamRoute.Name()}' must return rate limiting headers with the following data: {headers}");
        }

        return _next.Invoke(context);
    }

    protected virtual Task BreakExecution(HttpContext context, RateLimitOptions options, double retryAfter)
    {
        var retryAfterHeader = retryAfter.ToString(CultureInfo.InvariantCulture);
        var ds = ReturnQuotaExceededResponse(context, options, retryAfterHeader);
        context.Items.UpsertDownstreamResponse(ds);
        var error = new QuotaExceededError(GetResponseMessage(options), options.StatusCode);
        context.Items.SetError(error);
        return Task.CompletedTask;
    }

    protected virtual ClientRequestIdentity Identify(HttpContext context, RateLimitOptions options, DownstreamRoute route)
    {
        var clientId = RateLimitOptions.DefaultClientHeader;
        if (context.Request.Headers.TryGetValue(options.ClientIdHeader, out var headerValue))
        {
            clientId = headerValue.First();
        }

        return new ClientRequestIdentity(clientId, route.LoadBalancerKey);
    }

    public static bool IsWhitelisted(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        => option.ClientWhitelist.Contains(requestIdentity.ClientId);

    public virtual void LogBlockedRequest(HttpContext context, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule, DownstreamRoute route)
    {
        var req = context.Request;
        Logger.LogInformation(
            () => $"Request {req.Method} {req.Path} from ClientId {identity.ClientId} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.Total}. Blocked by rule '{rule}' of the route '{route.Name()}' with {nameof(HttpContext.TraceIdentifier)}:{context.TraceIdentifier}.");
    }

    public virtual DownstreamResponse ReturnQuotaExceededResponse(HttpContext context, RateLimitOptions options, string retryAfter)
    {
        var message = GetResponseMessage(options);
        var http = new HttpResponseMessage((HttpStatusCode)options.StatusCode)
        {
            Content = new StringContent(message),
        };

        if (options.EnableHeaders)
        {
            http.Headers.TryAddWithoutValidation(HeaderNames.RetryAfter, retryAfter); // in seconds, not date string
            context.Response.Headers.RetryAfter = retryAfter;
        }

        return new DownstreamResponse(http);
    }

    protected virtual string GetResponseMessage(RateLimitOptions options)
    {
        var format = options.QuotaMessage.IfEmpty(RateLimitOptions.DefaultQuotaMessage);
        return string.Format(format, options.Rule.Limit, options.Rule.Period);
    }

    /// <summary>TODO: Produced Ocelot's headers don't follow industry standards.</summary>
    /// <remarks>More details in <see cref="RateLimitingHeaders"/> docs.</remarks>
    /// <param name="state">Captured state as a <see cref="RateLimitHeaders"/> object.</param>
    /// <returns>A <see cref="Task.CompletedTask"/> object.</returns>
    protected virtual Task SetRateLimitHeaders(object state)
    {
        var limitHeaders = (RateLimitHeaders)state;
        var headers = limitHeaders.Context.Response.Headers;
        headers[RateLimitingHeaders.X_RateLimit_Limit] = new StringValues(limitHeaders.Limit.ToString());
        headers[RateLimitingHeaders.X_RateLimit_Remaining] = new StringValues(limitHeaders.Remaining.ToString());
        headers[RateLimitingHeaders.X_RateLimit_Reset] = new StringValues(limitHeaders.Reset.ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
        return Task.CompletedTask;
    }
}
