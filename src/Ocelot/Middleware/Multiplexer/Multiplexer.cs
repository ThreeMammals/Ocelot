using System.Threading.Tasks;
using Ocelot.Configuration;

namespace Ocelot.Middleware.Multiplexer
{
    public interface IMultiplexer
    {
        Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next);
    }

    public class Multiplexer : IMultiplexer
    {
        public async Task Multiplex(DownstreamContext context, ReRoute reRoute, OcelotRequestDelegate next)
        {
            var tasks = new Task<DownstreamContext>[reRoute.DownstreamReRoute.Count];
            for (int i = 0; i < reRoute.DownstreamReRoute.Count; i++)
            {
                var downstreamContext = new DownstreamContext(context.HttpContext)
                {
                    TemplatePlaceholderNameAndValues = context.TemplatePlaceholderNameAndValues,
                    ServiceProviderConfiguration = context.ServiceProviderConfiguration,
                    DownstreamReRoute = reRoute.DownstreamReRoute[i],
                    //todo do we want these set here
                    RequestId = context.RequestId,
                    PreviousRequestId = context.PreviousRequestId,
                };

                tasks[i] = Fire(downstreamContext, next);
            }

            await Task.WhenAll(tasks);

            //now cast the complete tasks to whatever they need to be
            //store them and let the response middleware handle them..

            var finished = tasks[0].Result;

            context.Errors = finished.Errors;
            context.DownstreamRequest = finished.DownstreamRequest;
            context.DownstreamResponse = finished.DownstreamResponse;
            context.RequestId = finished.RequestId;
            context.PreviousRequestId = finished.RequestId;
        }

        private async Task<DownstreamContext> Fire(DownstreamContext context, OcelotRequestDelegate next)
        {
            await next.Invoke(context);
            return context;
        }
    }
}
