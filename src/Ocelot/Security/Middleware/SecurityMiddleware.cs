namespace Ocelot.Security.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;

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

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var downstreamReRoute = Get(httpContext, downstreamContext);

            if (_securityPolicies != null)
            {
                foreach (var policy in _securityPolicies)
                {
                    var result = await policy.Security(downstreamReRoute, httpContext);
                    if (!result.IsError)
                    {
                        continue;
                    }

                    SetPipelineError(downstreamContext, result.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);

            httpContext.Items.SetDownstreamRequest(downstreamContext.DownstreamRequest);
            httpContext.Items.SetDownstreamResponse(downstreamContext.DownstreamResponse);
        }
    }
}
