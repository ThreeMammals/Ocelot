using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using Ocelot.Configuration;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Globalization;

namespace Ocelot.RateLimiting.Middleware;

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

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRoute = httpContext.Items.DownstreamRoute();

        var options = downstreamRoute.RateLimitOptions ?? new(false);
        if (!options.EnableRateLimiting)
        {
            Logger.LogInformation(() => $"Rate limiting is disabled for route '{downstreamRoute.Name()}' via the {nameof(RateLimitOptions.EnableRateLimiting)} option.");
            await _next.Invoke(httpContext);
            return;
        }

        // compute identity from request
        var identity = SetIdentity(httpContext, options);

        // check white list
        if (IsWhitelisted(identity, options))
        {
            Logger.LogInformation(() => $"Route '{downstreamRoute.Name()}' is configured to bypass rate limiting based on the client's header, due to the client's ID being detected in the whitelist.");
            await _next.Invoke(httpContext);
            return;
        }

        var rule = options.RateLimitRule;
        if (rule.Limit > 0)
        {
            // increment counter
            var counter = _limiter.ProcessRequest(identity, options);

            // check if limit is reached
            if (counter.TotalRequests > rule.Limit)
            {
                var retryAfter = _limiter.RetryAfter(counter, rule); // compute retry after value based on counter state
                LogBlockedRequest(httpContext, identity, counter, rule, downstreamRoute); // log blocked request virtually

                // break execution
                var ds = ReturnQuotaExceededResponse(httpContext, options, retryAfter.ToString(CultureInfo.InvariantCulture));
                httpContext.Items.UpsertDownstreamResponse(ds);

                // Set Error
                httpContext.Items.SetError(new QuotaExceededError(GetResponseMessage(options), options.StatusCode));
                return;
            }
        }

        // Set X-Rate-Limit-* headers for the longest period
        if (options.EnableHeaders)
        {
            var originalContext = _contextAccessor?.HttpContext;
            if (originalContext != null)
            {
                var headers = _limiter.GetHeaders(originalContext, identity, options);
                originalContext.Response.OnStarting(SetRateLimitHeaders, state: headers);
            }
        }

        await _next.Invoke(httpContext);
    }

    public virtual ClientRequestIdentity SetIdentity(HttpContext httpContext, RateLimitOptions option)
    {
        var clientId = RateLimitOptions.DefaultClientHeader;
        if (httpContext.Request.Headers.TryGetValue(option.ClientIdHeader, out var headerValue))
        {
            clientId = headerValue.First();
        }

        var req = httpContext.Request;
        return new ClientRequestIdentity(clientId,
            req.Path.ToString().ToLowerInvariant(),
            req.Method.ToUpperInvariant());
    }

    public static bool IsWhitelisted(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        => option.ClientWhitelist.Contains(requestIdentity.ClientId);

    public virtual void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule, DownstreamRoute downstreamRoute)
    {
        Logger.LogInformation(
            () => $"Request {identity.HttpVerb}:{identity.Path} from ClientId {identity.ClientId} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.TotalRequests}. Blocked by rule {downstreamRoute.UpstreamPathTemplate.OriginalValue}, TraceIdentifier {httpContext.TraceIdentifier}.");
    }

    public virtual DownstreamResponse ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitOptions option, string retryAfter)
    {
        var message = GetResponseMessage(option);
        var http = new HttpResponseMessage((HttpStatusCode)option.StatusCode)
        {
            Content = new StringContent(message),
        };

        if (option.EnableHeaders)
        {
            http.Headers.TryAddWithoutValidation(HeaderNames.RetryAfter, retryAfter); // in seconds, not date string
            httpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfter;
        }

        return new DownstreamResponse(http);
    }

    protected virtual string GetResponseMessage(RateLimitOptions option)
    {
        var format = option.QuotaExceededMessage.IfEmpty(RateLimitOptions.DefaultQuotaMessage);
        return string.Format(format, option.RateLimitRule.Limit, option.RateLimitRule.Period);
    }

    /// <summary>TODO: Produced Ocelot's headers don't follow industry standards.</summary>
    /// <remarks>More details in <see cref="RateLimitingHeaders"/> docs.</remarks>
    /// <param name="state">Captured state as a <see cref="RateLimitHeaders"/> object.</param>
    /// <returns>A <see cref="Task.CompletedTask"/> object.</returns>
    protected virtual Task SetRateLimitHeaders(object state)
    {
        var limitHeaders = (RateLimitHeaders)state;
        var headers = limitHeaders.Context.Response.Headers;
        headers[RateLimitingHeaders.X_Rate_Limit_Limit] = new StringValues(limitHeaders.Limit.ToString());
        headers[RateLimitingHeaders.X_Rate_Limit_Remaining] = new StringValues(limitHeaders.Remaining.ToString());
        headers[RateLimitingHeaders.X_Rate_Limit_Reset] = new StringValues(limitHeaders.Reset.ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo));
        return Task.CompletedTask;
    }
}
