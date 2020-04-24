namespace Ocelot.DownstreamPathManipulation.Middleware
{
    using System.Linq;
    using System.Threading.Tasks;
    using Ocelot.Logging;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;
    using Ocelot.PathManipulation;

    public class ClaimsToDownstreamPathMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IChangeDownstreamPathTemplate _changeDownstreamPathTemplate;

        public ClaimsToDownstreamPathMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IChangeDownstreamPathTemplate changeDownstreamPathTemplate)
                : base(loggerFactory.CreateLogger<ClaimsToDownstreamPathMiddleware>())
        {
            _next = next;
            _changeDownstreamPathTemplate = changeDownstreamPathTemplate;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var downstreamReRoute = Get(httpContext, downstreamContext);

            if (downstreamReRoute.ClaimsToPath.Any())
            {
                Logger.LogInformation($"{downstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to path");
                var response = _changeDownstreamPathTemplate.ChangeDownstreamPath(downstreamReRoute.ClaimsToPath, httpContext.User.Claims,
                    downstreamReRoute.DownstreamPathTemplate, downstreamContext.TemplatePlaceholderNameAndValues);

                if (response.IsError)
                {
                    Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(downstreamContext, response.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
