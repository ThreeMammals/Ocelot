using Ocelot.Logging;
using Ocelot.Middleware;
using System.Linq;
using System.Threading.Tasks;

namespace Ocelot.PathManipulation.Middleware
{
    public class ClaimsToDownstreamPathMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly IChangeDownstreamPathTemplate _changeDownstreamPathTemplate;

        public ClaimsToDownstreamPathMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IChangeDownstreamPathTemplate changeDownstreamPathTemplate)
                : base(loggerFactory.CreateLogger<ClaimsToDownstreamPathMiddleware>())
        {
            _next = next;
            _changeDownstreamPathTemplate = changeDownstreamPathTemplate;
        }

        public async Task Invoke(DownstreamContext context)
        {
            if (context.DownstreamReRoute.ClaimsToPath.Any())
            {
                Logger.LogInformation($"{context.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to path");
                var response = _changeDownstreamPathTemplate.ChangeDownstreamPath(context.DownstreamReRoute.ClaimsToPath, context.HttpContext.User.Claims,
                                    context.DownstreamReRoute.DownstreamPathTemplate, context.TemplatePlaceholderNameAndValues);

                if (response.IsError)
                {
                    Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(context, response.Errors);
                    return;
                }
            }

            await _next.Invoke(context);
        }
    }
}
