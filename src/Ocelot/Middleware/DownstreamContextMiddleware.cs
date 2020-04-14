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
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;
        public DownstreamContextMiddleware(RequestDelegate next, IOcelotLogger logger, IRequestScopedDataRepository requestScopedDataRepository, IInternalConfigurationRepository configRepo) : base(logger, requestScopedDataRepository)
        {
            _next = next;
            _configRepo = configRepo;
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            //todo check the config is actually ok?
            var config = _configRepo.Get();

            var downstreamContext = new DownstreamContext {Configuration = config.Data};

            _requestScopedDataRepository.Add("DownstreamContext", downstreamContext);

            await _next.Invoke(httpContext);
        }
    }
}
