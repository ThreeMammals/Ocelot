using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.RateLimit.Middleware
{
    public class ClientRateLimitMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IRateLimitCounterHandler _counterHandler;
        private readonly ClientRateLimitProcessor _processor;

        public ClientRateLimitMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRateLimitCounterHandler counterHandler)
                : base(loggerFactory.CreateLogger<ClientRateLimitMiddleware>())
        {
            _next = next;
            _counterHandler = counterHandler;
            _processor = new ClientRateLimitProcessor(counterHandler);
        }

        public async Task Invoke(DownstreamContext context)
        {
            var options = context.DownstreamReRoute.RateLimitOptions;

            // check if rate limiting is enabled
            if (!context.DownstreamReRoute.EnableEndpointEndpointRateLimiting)
            {
                Logger.LogInformation($"EndpointRateLimiting is not enabled for {context.DownstreamReRoute.DownstreamPathTemplate.Value}");
                await _next.Invoke(context);
                return;
            }

            // compute identity from request
            var identity = SetIdentity(context.HttpContext, options);

            // check white list
            if (IsWhitelisted(identity, options))
            {
                Logger.LogInformation($"{context.DownstreamReRoute.DownstreamPathTemplate.Value} is white listed from rate limiting");
                await _next.Invoke(context);
                return;
            }

            var rule = options.RateLimitRule;
            if (rule.Limit > 0)
            {
                // increment counter
                var counter = _processor.ProcessRequest(identity, options);

                // check if limit is reached
                if (counter.TotalRequests > rule.Limit)
                {
                    //compute retry after value
                    var retryAfter = _processor.RetryAfterFrom(counter.Timestamp, rule);

                    // log blocked request
                    LogBlockedRequest(context.HttpContext, identity, counter, rule, context.DownstreamReRoute);

                    var retrystring = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    // break execution
                    await ReturnQuotaExceededResponse(context.HttpContext, options, retrystring);

                    // Set Error
                    context.Errors.Add(new QuotaExceededError(this.GetResponseMessage(options)));

                    return;
                }
            }

            //set X-Rate-Limit headers for the longest period
            if (!options.DisableRateLimitHeaders)
            {
                var headers = _processor.GetRateLimitHeaders(context.HttpContext, identity, options);
                context.HttpContext.Response.OnStarting(SetRateLimitHeaders, state: headers);
            }

            await _next.Invoke(context);
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

        public bool IsWhitelisted(ClientRequestIdentity requestIdentity, RateLimitOptions option)
        {
            if (option.ClientWhitelist.Contains(requestIdentity.ClientId))
            {
                return true;
            }

            return false;
        }

        public virtual void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule, DownstreamReRoute downstreamReRoute)
        {
            Logger.LogInformation(
                $"Request {identity.HttpVerb}:{identity.Path} from ClientId {identity.ClientId} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.TotalRequests}. Blocked by rule { downstreamReRoute.UpstreamPathTemplate.OriginalValue }, TraceIdentifier {httpContext.TraceIdentifier}.");
        }

        public virtual Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitOptions option, string retryAfter)
        {
            var message = this.GetResponseMessage(option);

            if (!option.DisableRateLimitHeaders)
            {
                httpContext.Response.Headers["Retry-After"] = retryAfter;
            }

            httpContext.Response.StatusCode = option.HttpStatusCode;
            return httpContext.Response.WriteAsync(message);
        }

        private string GetResponseMessage(RateLimitOptions option)
        {
            var message = string.IsNullOrEmpty(option.QuotaExceededMessage)
                ? $"API calls quota exceeded! maximum admitted {option.RateLimitRule.Limit} per {option.RateLimitRule.Period}."
                : option.QuotaExceededMessage;
            return message;
        }

        private Task SetRateLimitHeaders(object rateLimitHeaders)
        {
            var headers = (RateLimitHeaders)rateLimitHeaders;

            headers.Context.Response.Headers["X-Rate-Limit-Limit"] = headers.Limit;
            headers.Context.Response.Headers["X-Rate-Limit-Remaining"] = headers.Remaining;
            headers.Context.Response.Headers["X-Rate-Limit-Reset"] = headers.Reset;

            return Task.CompletedTask;
        }
    }
}
