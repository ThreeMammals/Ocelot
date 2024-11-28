using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Ocelot.Configuration;
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

        var options = downstreamRoute.RateLimitOptions;

        // check if rate limiting is enabled
        if (!downstreamRoute.EnableEndpointEndpointRateLimiting)
        {
            Logger.LogInformation(() => $"EndpointRateLimiting is not enabled for {downstreamRoute.DownstreamPathTemplate.Value}");
            await _next.Invoke(httpContext);
            return;
        }

        // compute identity from request
        var identity = SetIdentity(httpContext, options);

        // check white list
        if (IsWhitelisted(identity, options))
        {
            Logger.LogInformation(() => $"{downstreamRoute.DownstreamPathTemplate.Value} is white listed from rate limiting");
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
                httpContext.Items.SetError(new QuotaExceededError(GetResponseMessage(options), options.HttpStatusCode));
                return;
            }
        }

        // Set X-Rate-Limit headers for the longest period
        if (!options.DisableRateLimitHeaders)
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
        var clientId = "client";
        if (httpContext.Request.Headers.Keys.Contains(option.ClientIdHeader))
        {
            clientId = httpContext.Request.Headers[option.ClientIdHeader].First();
        }

        return new ClientRequestIdentity(
            clientId,
            httpContext.Request.Path.ToString().ToLowerInvariant(),
            httpContext.Request.Method.ToLowerInvariant()
            );
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
        var http = new HttpResponseMessage((HttpStatusCode)option.HttpStatusCode)
        {
            Content = new StringContent(message),
        };

        if (!option.DisableRateLimitHeaders)
        {
            http.Headers.TryAddWithoutValidation(HeaderNames.RetryAfter, retryAfter); // in seconds, not date string
            httpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfter;
        }

        return new DownstreamResponse(http);
    }

    private static string GetResponseMessage(RateLimitOptions option)
    {
        var message = string.IsNullOrEmpty(option.QuotaExceededMessage)
            ? $"API calls quota exceeded! maximum admitted {option.RateLimitRule.Limit} per {option.RateLimitRule.Period}."
            : option.QuotaExceededMessage;
        return message;
    }

    /// <summary>TODO: Produced Ocelot's headers don't follow industry standards.</summary>
    /// <remarks>More details in <see cref="RateLimitingHeaders"/> docs.</remarks>
    /// <param name="state">Captured state as a <see cref="RateLimitHeaders"/> object.</param>
    /// <returns>The <see cref="Task.CompletedTask"/> object.</returns>
    private static Task SetRateLimitHeaders(object state)
    {
        var limitHeaders = (RateLimitHeaders)state;
        var headers = limitHeaders.Context.Response.Headers;
        headers[RateLimitingHeaders.X_Rate_Limit_Limit] = limitHeaders.Limit;
        headers[RateLimitingHeaders.X_Rate_Limit_Remaining] = limitHeaders.Remaining;
        headers[RateLimitingHeaders.X_Rate_Limit_Reset] = limitHeaders.Reset;
        return Task.CompletedTask;
    }
}
