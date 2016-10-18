using Ocelot.Library.ScopedData;

namespace Ocelot.Library.Middleware
{
    using System.Collections.Generic;
    using Errors;

    public abstract class OcelotMiddleware
    {
        private readonly IScopedRequestDataRepository _scopedRequestDataRepository;

        protected OcelotMiddleware(IScopedRequestDataRepository scopedRequestDataRepository)
        {
            _scopedRequestDataRepository = scopedRequestDataRepository;
        }

        public void SetPipelineError(List<Error> errors)
        {
            _scopedRequestDataRepository.Add("OcelotMiddlewareError", true);
            _scopedRequestDataRepository.Add("OcelotMiddlewareErrors", errors);
        }

        public bool PipelineError()
        {
            var response = _scopedRequestDataRepository.Get<bool>("OcelotMiddlewareError");
            return response.Data;
        }

        public List<Error> GetPipelineErrors()
        {
            var response = _scopedRequestDataRepository.Get<List<Error>>("OcelotMiddlewareErrors");
            return response.Data;
        }
    }
}
