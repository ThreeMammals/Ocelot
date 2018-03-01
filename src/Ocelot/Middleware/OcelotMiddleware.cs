using System.Collections.Generic;
using Ocelot.Errors;

namespace Ocelot.Middleware
{
    public abstract class OcelotMiddleware
    {
        protected OcelotMiddleware()
        {
            MiddlewareName = this.GetType().Name;
        }

        public string MiddlewareName { get; }

        public void SetPipelineError(DownstreamContext context, List<Error> errors)
        {
            context.Errors = errors;
        }
    }
}
