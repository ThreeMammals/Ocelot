namespace Ocelot.Request.Middleware
{
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Ocelot.Request.Creator;
    using System.Threading.Tasks;

    public class DownstreamRequestInitialiserMiddleware : OcelotMiddleware
    {
        private readonly OcelotRequestDelegate _next;
        private readonly Mapper.IRequestMapper _requestMapper;
        private readonly IDownstreamRequestCreator _creator;

        public DownstreamRequestInitialiserMiddleware(OcelotRequestDelegate next,
            IOcelotLoggerFactory loggerFactory,
            Mapper.IRequestMapper requestMapper,
            IDownstreamRequestCreator creator)
                : base(loggerFactory.CreateLogger<DownstreamRequestInitialiserMiddleware>())
        {
            _next = next;
            _requestMapper = requestMapper;
            _creator = creator;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var downstreamRequest = await _requestMapper.Map(context.HttpContext.Request);

            if (downstreamRequest.IsError)
            {
                SetPipelineError(context, downstreamRequest.Errors);
                return;
            }

            context.DownstreamRequest = _creator.Create(downstreamRequest.Data);

            await _next.Invoke(context);
        }
    }
}
