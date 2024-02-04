using Microsoft.AspNetCore.Http;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.Security.Middleware
{
    public class SecurityMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IEnumerable<ISecurityPolicy> _securityPolicies;

        public SecurityMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IEnumerable<ISecurityPolicy> securityPolicies
            )
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
                    var result = await policy.Security(downstreamRoute, httpContext);
                    if (!result.IsError)
                    {
                        continue;
                    }

                    httpContext.Items.UpsertErrors(result.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
