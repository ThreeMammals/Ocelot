namespace Ocelot.Middleware
{
    using Ocelot.Infrastructure.RequestData;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Errors;
    using Ocelot.Logging;
    using System.Collections.Generic;
    using Configuration;
    using Responses;

    public abstract class OcelotMiddleware
    {
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        protected OcelotMiddleware(IOcelotLogger logger, 
            IRequestScopedDataRepository requestScopedDataRepository
            )
        {
            Logger = logger;
            _requestScopedDataRepository = requestScopedDataRepository;
            MiddlewareName = GetType().Name;
        }

        public IOcelotLogger Logger { get; }
        public string MiddlewareName { get; }

        //todo check data is ok
        public Response<IInternalConfiguration> Configuration => _requestScopedDataRepository.Get<IInternalConfiguration>("IInternalConfiguration");

        public Response<DownstreamContext> DownstreamContext => _requestScopedDataRepository.Get<DownstreamContext>("DownstreamContext");

        public Response<List<Error>> Errors => _requestScopedDataRepository.Get<List<Error>>("Errors");

        public void SetPipelineError(HttpContext context, List<Error> errors)
        {
            foreach (var error in errors)
            {
                SetPipelineError(context, error);
            }
        }

        public void SetPipelineError(HttpContext context, Error error)
        {
            Logger.LogWarning(error.Message);
            // context.Errors.Add(error);

            // todo write tests for this
            var errors = RequestScopedDataRepository.Get<List<Error>>("Errors");

            if (!errors.IsError)
            {
                errors.Data.Add(error);
                RequestScopedDataRepository.Update("Errors", errors);
            }
            else
            {
                RequestScopedDataRepository.Add("Errors", new List<Error> { error });
            }
        }
    }
}
