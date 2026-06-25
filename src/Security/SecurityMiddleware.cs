using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Security;

public class SecurityMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<ISecurityPolicy> _securityPolicies;

    public SecurityMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IEnumerable<ISecurityPolicy> securityPolicies)
        : base(loggerFactory.CreateLogger<SecurityMiddleware>())
    {
        _securityPolicies = securityPolicies;
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var downstreamRoute = httpContext.Items.DownstreamRoute();

        if (_securityPolicies != null)
        {
            foreach (var policy in _securityPolicies)
            {
                var result = policy.Security(downstreamRoute, httpContext);
                if (!result.IsError)
                {
                    continue;
                }

                httpContext.Items.UpsertErrors(result.Errors);

                // The WebSocket pipeline has no ResponderMiddleware to translate this error into a status code,
                // so the upgrade would otherwise complete as 200 OK. Per RFC 6455 (§4.1) a declined upgrade must
                // return a non-101 status, so reject it with 403 Forbidden. fix #2403
                if (httpContext.WebSockets.IsWebSocketRequest)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                }

                return;
            }
        }

        await _next.Invoke(httpContext);
    }
}
