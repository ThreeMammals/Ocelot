using Ocelot.Errors;
using Ocelot.Logging;
using System.Collections.Generic;

namespace Ocelot.Middleware
{
    public abstract class OcelotMiddleware
    {
        protected OcelotMiddleware(IOcelotLogger logger)
        {
            Logger = logger;
            MiddlewareName = this.GetType().Name;
        }

        public IOcelotLogger Logger { get; }

        public string MiddlewareName { get; }

        public void SetPipelineError(DownstreamContext context, List<Error> errors)
        {
            foreach (var error in errors)
            {
                SetPipelineError(context, error);
            }
        }

        public void SetPipelineError(DownstreamContext context, Error error)
        {
            Logger.LogWarning(error.Message);
            context.Errors.Add(error);
        }
    }
}
