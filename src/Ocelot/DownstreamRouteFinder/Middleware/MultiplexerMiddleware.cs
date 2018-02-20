using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ocelot.Middleware;

namespace Ocelot.DownstreamRouteFinder.Middleware
{
    public class MultiplexerMiddleware : OcelotMiddlewareV2
    {
        private readonly OcelotRequestDelegate _realNext;
        private List<Thread> _threads;

        protected MultiplexerMiddleware(OcelotRequestDelegate realNext)
        {
            _realNext = realNext;
            _threads = new List<Thread>();
        }

        public async Task Invoke(DownstreamContext context)
        {
            var tasks = new Task[context.DownstreamRoute.ReRoute.DownstreamReRoute.Count];

            for (int i = 0; i < context.DownstreamRoute.ReRoute.DownstreamReRoute.Count; i++)
            {
                //todo this is now a mess
                //var downstreamContext = new DownstreamContext(context.HttpContext, context.DownstreamRoute.ReRoute, context.DownstreamRoute.TemplatePlaceholderNameAndValues, context.ServiceProviderConfiguration, context.DownstreamRoute.ReRoute.DownstreamReRoute[i]);

                tasks[i] = _realNext.Invoke(context);
            }

            Task.WaitAll(tasks);

            //now cast the complete tasks to whatever they need to be
            //store them and let the response middleware handle them..
        }
    }
}
