namespace Ocelot.Request.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Request.Creator;
    using System.Threading.Tasks;
    using Configuration;
    using Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;

    public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Mapper.IRequestMapper _requestMapper;
        private readonly IDownstreamRequestCreator _creator;

        public DownstreamRequestInitialiserMiddleware(RequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            Mapper.IRequestMapper requestMapper,
            IDownstreamRequestCreator creator,
            IRequestScopedDataRepository repo)
                : base(loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>(), repo)
        {
            _next = next;
            _requestMapper = requestMapper;
            _creator = creator;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var httpRequestMessage = await _requestMapper.Map(httpContext.Request, DownstreamContext.Data.DownstreamReRoute);

            if (httpRequestMessage.IsError)
            {
                SetPipelineError(httpContext, httpRequestMessage.Errors);
                return;
            }

            var downstreamRequest = _creator.Create(httpRequestMessage.Data);

            DownstreamContext.Data.DownstreamRequest = downstreamRequest;

            await _next.Invoke(httpContext);
        }
    }
}
