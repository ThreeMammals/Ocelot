namespace Ocelot.Middleware
{
    using Ocelot.Errors;
    using Ocelot.Logging;
    using System.Collections.Generic;

    public abstract class OcelotMiddleware
    {
        protected OcelotMiddleware(IOcelotLogger logger)
        {
            Logger = logger;
            MiddlewareName = GetType().Name;
        }

        public IOcelotLogger Logger { get; }
        public string MiddlewareName { get; }

        public void SetPipelineError(IDownstreamContext downstreamContext, List<Error> errors)
        {
            foreach (var error in errors)
            {
                SetPipelineError(downstreamContext, error);
            }
        }

        public void SetPipelineError(IDownstreamContext downstreamContext, Error error)
        {
            Logger.LogWarning(error.Message);
            downstreamContext.Errors.Add(error);
        }
    }
}
