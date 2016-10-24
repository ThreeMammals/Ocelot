using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;

namespace Ocelot.Middleware
{
    public abstract class OcelotMiddleware
    {
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        protected OcelotMiddleware(IRequestScopedDataRepository requestScopedDataRepository)
        {
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public void SetPipelineError(List<Error> errors)
        {
            _requestScopedDataRepository.Add("OcelotMiddlewareError", true);
            _requestScopedDataRepository.Add("OcelotMiddlewareErrors", errors);
        }

        public bool PipelineError()
        {
            var response = _requestScopedDataRepository.Get<bool>("OcelotMiddlewareError");
            return response.Data;
        }

        public List<Error> GetPipelineErrors()
        {
            var response = _requestScopedDataRepository.Get<List<Error>>("OcelotMiddlewareErrors");
            return response.Data;
        }
    }
}
