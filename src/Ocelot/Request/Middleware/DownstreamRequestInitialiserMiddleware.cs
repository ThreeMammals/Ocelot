namespace Ocelot.Request.Middleware
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.Infrastructure.RequestData;
    using Ocelot.Logging;
    using Ocelot.Middleware;

    public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly Mapper.IRequestMapper _requestMapper;

        public DownstreamRequestInitialiserMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            Mapper.IRequestMapper requestMapper)
                :base(loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>())
        {
            _next = next;
            _requestMapper = requestMapper;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var downstreamRequest = await _requestMapper.Map(context.HttpContext.Request);
            if (downstreamRequest.IsError)
            {
                SetPipelineError(context, downstreamRequest.Errors);
                return;
            }

            context.DownstreamRequest = new DownstreamRequest(downstreamRequest.Data);

            await _next.Invoke(context);
        }
    }
}
