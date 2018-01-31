namespace Ocelot.Request.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    using Ocelot.Infrastructure.RequestData;
    using Ocelot.Logging;
    using Ocelot.Middleware;

    public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IOcelotLogger _logger;
        private readonly Mapper.IRequestMapper _requestMapper;

        public DownstreamRequestInitialiserMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            IRequestScopedDataRepository requestScopedDataRepository,
            Mapper.IRequestMapper requestMapper)
            :base(requestScopedDataRepository)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>();
            _requestMapper = requestMapper;
        }

        public async Task Invoke(HttpContext context)
        {
            var downstreamRequest = await _requestMapper.Map(context.Request);
            if (downstreamRequest.IsError)
            {
                SetPipelineError(downstreamRequest.Errors);
                return;
            }

            SetDownstreamRequest(downstreamRequest.Data);

            await _next.Invoke(context);
        }
    }
}