using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.Repository;
using Ocelot.Logging;

namespace Ocelot.Middleware
{
    public class ConfigurationMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IInternalConfigurationRepository _configRepo;

        public ConfigurationMiddleware(
            RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IInternalConfigurationRepository configRepo)
            : base(loggerFactory.CreateLogger<ConfigurationMiddleware>())
        {
            _next = next;
            _configRepo = configRepo;
        }

        protected override string MiddlewareName => nameof(ConfigurationMiddleware);

        public async Task Invoke(HttpContext httpContext)
        {
            //todo check the config is actually ok?
            var config = _configRepo.Get();

            if (config.IsError)
            {
                throw new System.Exception("OOOOPS this should not happen raise an issue in GitHub");
            }

            httpContext.Items.SetIInternalConfiguration(config.Data);

            await _next.Invoke(httpContext);
        }
    }
}
