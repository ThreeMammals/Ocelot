using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Security;

public class SecurityMiddleware : OcelotMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<ISecurityPolicy> _policies;

    public SecurityMiddleware(RequestDelegate next,
        IOcelotLoggerFactory loggerFactory,
        IEnumerable<ISecurityPolicy> policies)
        : base(loggerFactory.CreateLogger<SecurityMiddleware>())
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(policies);
        _policies = policies;
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var downstreamRoute = context.Items.DownstreamRoute();
        foreach (var policy in _policies)
        {
            var result = policy.Security(downstreamRoute, context);
            if (result.IsError)
            {
                context.Items.UpsertErrors(result.Errors);
                HandleWebSocketErrors(context, result.Errors);
                return; // go to ResponderMiddleware and it takes the rest of responsibility
            }
        }
        await _next.Invoke(context);
    }

    /// <summary>
    /// Handles WebSocket requests in case of an error.
    /// Overrides the status code to 403 Forbidden.
    /// </summary>
    /// <remarks>Links:<br/>
    /// 1. RFC 6455 §4.2.2. <see href="https://www.rfc-editor.org/info/rfc6455/#section-4.2.2">Sending the Server's Opening Handshake</see> (point 4).<br/>
    /// 2. RFC 6455 §10.2. <see href="https://www.rfc-editor.org/info/rfc6455/#section-10.2">Security Considerations | Origin Considerations</see>.
    /// </remarks>
    /// <param name="context">The HTTP context being processed.</param>
    /// <param name="errors">The Ocelot errors to be processed.</param>
    protected static void HandleWebSocketErrors(HttpContext context, List<Error> errors)
    {
        // The WebSocket pipeline has no ResponderMiddleware to translate this error into a status code, so the upgrade would otherwise complete as 200 OK.
        // Per RFC 6455 (§4.2.2) a declined upgrade must return a non-101 status, so reject it with 403 Forbidden.
        if (context.WebSockets.IsWebSocketRequest)
            context.Response.StatusCode = StatusCodes.Status403Forbidden; // https://www.rfc-editor.org/info/rfc6455/#section-4.2.2:~:text=403%20Forbidden
    }
}
