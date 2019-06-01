using Ocelot.Logging;
using Ocelot.Middleware;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocelot.Security.Middleware
{
    public class SecurityMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly IEnumerable<ISecurityPolicy> _securityPolicies;

        public SecurityMiddleware(IOcelotLoggerFactory loggerFactory,
            IEnumerable<ISecurityPolicy> securityPolicies,
            OcelotRequestDelegate next)
            : base(loggerFactory.CreateLogger<SecurityMiddleware>())
        {
            _logger = loggerFactory.CreateLogger<SecurityMiddleware>();
            _securityPolicies = securityPolicies;
            _next = next;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (_securityPolicies != null)
            {
                foreach (var policie in _securityPolicies)
                {
                    var result = await policie.Security(context);
                    if (!result.IsError)
                    {
                        continue;
                    }

                    this.SetPipelineError(context, result.Errors);
                    return;
                }
            }

            await _next.Invoke(context);
        }
    }
}
