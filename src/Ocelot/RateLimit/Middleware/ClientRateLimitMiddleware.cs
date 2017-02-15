using Ocelot.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.Infrastructure.RequestData;
using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Configuration;

namespace Ocelot.RateLimit.Middleware
{
    public class ClientRateLimitMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IRateLimitCounterHandler _counterHandler;
        private readonly ClientRateLimitProcessor _processor;

        public ClientRateLimitMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            IRateLimitCounterHandler counterHandler)
            : base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<ClientRateLimitMiddleware>();
            _counterHandler = counterHandler;
            _processor = new ClientRateLimitProcessor(counterHandler);
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("started calling RateLimit middleware");
            var options = DownstreamRoute.ReRoute.RateLimitOptions;
            // check if rate limiting is enabled
            if (!DownstreamRoute.ReRoute.EnableEndpointRateLimiting)
            {
                await _next.Invoke(context);
                return;
            }
            // compute identity from request
            var identity = SetIdentity(context, options);

            // check white list
            if (IsWhitelisted(identity, options))
            {
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
                    LogBlockedRequest(context, identity, counter, rule);

                    // break execution
                    await ReturnQuotaExceededResponse(context, options, retryAfter);
                    return;
                }
            }
            //set X-Rate-Limit headers for the longest period
            if (!options.DisableRateLimitHeaders)
            {
                var headers = _processor.GetRateLimitHeaders( context,identity, options);
                context.Response.OnStarting(SetRateLimitHeaders, state: headers);
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

        public virtual void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
        {
            _logger.LogDebug($"Request {identity.HttpVerb}:{identity.Path} from ClientId {identity.ClientId} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.TotalRequests}. Blocked by rule { DownstreamRoute.ReRoute.UpstreamPathTemplate }, TraceIdentifier {httpContext.TraceIdentifier}.");
        }

        public virtual Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitOptions option, string retryAfter)
        {
            var message = string.IsNullOrEmpty(option.QuotaExceededMessage) ? $"API calls quota exceeded! maximum admitted {option.RateLimitRule.Limit} per {option.RateLimitRule.Period}." : option.QuotaExceededMessage;

            if (!option.DisableRateLimitHeaders)
            {
                httpContext.Response.Headers["Retry-After"] = retryAfter;
            }

            httpContext.Response.StatusCode = option.HttpStatusCode;
            return httpContext.Response.WriteAsync(message);
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


