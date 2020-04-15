namespace Ocelot.Middleware
{
    using System.Threading.Tasks;
    using Ocelot.Errors.Middleware;
    using Ocelot.Configuration.Repository;
    using Ocelot.Logging;
    using Microsoft.AspNetCore.Http;

    public class ConfigurationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IInternalConfigurationRepository _configRepo;

        public ConfigurationMiddleware(RequestDelegate next, IOcelotLoggerFactory loggerFactory,  IInternalConfigurationRepository configRepo) 
            : base(loggerFactory.CreateLogger<ExceptionHandlerMiddleware>())
        {
            _next = next;
            _configRepo = configRepo;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            //todo check the config is actually ok?
            var config = _configRepo.Get();

            downstreamContext.Configuration = config.Data;

            await _next.Invoke(httpContext);
        }
    }
}
