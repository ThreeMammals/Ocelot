using System.Threading.Tasks;
using Ocelot.Middleware;
using Ocelot.Middleware.Pipeline;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class MultiplexerMiddleware : OcelotMiddlewareV2
    {
        private readonly OcelotRequestDelegate _next;

        public MultiplexerMiddleware(OcelotRequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(DownstreamContext context)
        {
            var tasks = new Task<DownstreamContext>[context.DownstreamRoute.ReRoute.DownstreamReRoute.Count];
            for (int i = 0; i < context.DownstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
            {
                var downstreamContext = new DownstreamContext(context.HttpContext)
                {
                    DownstreamRoute = context.DownstreamRoute,
                    ServiceProviderConfiguration = context.ServiceProviderConfiguration,
                    DownstreamReRoute = context.DownstreamRoute.ReRoute.DownstreamReRoute[i],
                    //todo do we want these set here
                    RequestId = context.RequestId,
                    PreviousRequestId = context.PreviousRequestId,
                };

                tasks[i] = Fire(downstreamContext);
            }

            await Task.WhenAll(tasks);

            //now cast the complete tasks to whatever they need to be
            //store them and let the response middleware handle them..

            var finished = tasks[0].Result;

            context.Response = finished.Response;
            context.DownstreamRequest = finished.DownstreamRequest;
            context.DownstreamResponse = finished.DownstreamResponse;
            context.RequestId = finished.RequestId;
            context.PreviousRequestId = finished.RequestId;
        }

        private async Task<DownstreamContext> Fire(DownstreamContext context)
        {
            await _next.Invoke(context);
            return context;
        }
    }

    public static class MultiplexerMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseMultiplexerMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<MultiplexerMiddleware>();
        }
    }
}
