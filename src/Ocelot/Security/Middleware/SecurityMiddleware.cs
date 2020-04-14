using Ocelot.Logging;
using Ocelot.Middleware;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Security.Middleware
{
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;

    public class SecurityMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IEnumerable<ISecurityPolicy> _securityPolicies;

        public SecurityMiddleware(RequestDelegate next, 
            IOcelotLoggerFactory loggerFactory,
            IEnumerable<ISecurityPolicy> securityPolicies,
            IRequestScopedDataRepository repo
            )
            : base(loggerFactory.CreateLogger<SecurityMiddleware>(), repo)
        {
            _logger = loggerFactory.CreateLogger<SecurityMiddleware>();
            _securityPolicies = securityPolicies;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (_securityPolicies != null)
            {
                foreach (var policy in _securityPolicies)
                {
                    var result = await policy.Security(httpContext);
                    if (!result.IsError)
                    {
                        continue;
                    }

                    SetPipelineError(httpContext, result.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
