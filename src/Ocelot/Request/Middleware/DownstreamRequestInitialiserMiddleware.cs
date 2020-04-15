namespace Ocelot.Request.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Request.Creator;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Mapper.IRequestMapper _requestMapper;
        private readonly IDownstreamRequestCreator _creator;

        public DownstreamRequestInitialiserMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            Mapper.IRequestMapper requestMapper,
            IDownstreamRequestCreator creator)
                : base(loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>())
        {
            _next = next;
            _requestMapper = requestMapper;
            _creator = creator;
        }

        public async Task Invoke(HttpContext httpContext, IDownstreamContext downstreamContext)
        {
            var httpRequestMessage = await _requestMapper.Map(httpContext.Request, downstreamContext.DownstreamReRoute);

            if (httpRequestMessage.IsError)
            {
                SetPipelineError(downstreamContext, httpRequestMessage.Errors);
                return;
            }

            var downstreamRequest = _creator.Create(httpRequestMessage.Data);

            downstreamContext.DownstreamRequest = downstreamRequest;

            await _next.Invoke(httpContext);
        }
    }
}
