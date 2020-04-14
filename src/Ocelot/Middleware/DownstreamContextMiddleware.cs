namespace Ocelot.Middleware
{
    using System.Threading.Tasks;
    using Configuration.Repository;
    using Infrastructure.RequestData;
    using Logging;
    using Microsoft.AspNetCore.Http;

    public class DownstreamContextMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IInternalConfigurationRepository _configRepo;

        public DownstreamContextMiddleware(RequestDelegate next, IOcelotLogger logger, IRequestScopedDataRepository requestScopedDataRepository, IInternalConfigurationRepository configRepo) : base(logger, requestScopedDataRepository)
        {
            _next = next;
            _configRepo = configRepo;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var downstreamContext = new DownstreamContext();
            RequestScopedDataRepository.Add("DownstreamContext", downstreamContext);

            var config = _configRepo.Get();
            RequestScopedDataRepository.Add("IInternalConfiguration", config.Data);
            //todo check the config is actually ok?
            await _next.Invoke(httpContext);
        }
    }
}
