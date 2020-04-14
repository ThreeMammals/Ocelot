namespace Ocelot.DownstreamPathManipulation.Middleware
{
    using System.Linq;
    using System.Threading.Tasks;
    using Infrastructure.RequestData;
    using Logging;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Middleware;
    using PathManipulation;

    public class ClaimsToDownstreamPathMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IChangeDownstreamPathTemplate _changeDownstreamPathTemplate;

        public ClaimsToDownstreamPathMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IChangeDownstreamPathTemplate changeDownstreamPathTemplate,
            IRequestScopedDataRepository requestScopedDataRepository)
                : base(loggerFactory.CreateLogger<ClaimsToDownstreamPathMiddleware>(), requestScopedDataRepository)
        {
            _next = next;
            _changeDownstreamPathTemplate = changeDownstreamPathTemplate;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (DownstreamContext.Data.DownstreamReRoute.ClaimsToPath.Any())
            {
                Logger.LogInformation($"{DownstreamContext.Data.DownstreamReRoute.DownstreamPathTemplate.Value} has instructions to convert claims to path");
                var response = _changeDownstreamPathTemplate.ChangeDownstreamPath(DownstreamContext.Data.DownstreamReRoute.ClaimsToPath, httpContext.User.Claims,
                    DownstreamContext.Data.DownstreamReRoute.DownstreamPathTemplate, DownstreamContext.Data.TemplatePlaceholderNameAndValues);

                if (response.IsError)
                {
                    Logger.LogWarning("there was an error setting queries on context, setting pipeline error");

                    SetPipelineError(httpContext, response.Errors);
                    return;
                }
            }

            await _next.Invoke(httpContext);
        }
    }
}
