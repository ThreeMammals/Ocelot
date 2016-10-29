using System.Collections.Generic;
using Ocelot.DownstreamRouteFinder;
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

        public DownstreamRoute DownstreamRoute
        {
            get
            {
                var downstreamRoute = _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute");
                return downstreamRoute.Data;
            }
        }

        public string DownstreamUrl
        {
            get
            {
                var downstreamUrl = _requestScopedDataRepository.Get<string>("DownstreamUrl");
                return downstreamUrl.Data;
            }
        }

        public Request.Request Request
        {
            get
            {
                var request = _requestScopedDataRepository.Get<Request.Request>("Request");
                return request.Data;
            }
        }

        public void SetDownstreamRouteForThisRequest(DownstreamRoute downstreamRoute)
        {
            _requestScopedDataRepository.Add("DownstreamRoute", downstreamRoute);
        }

        public void SetDownstreamUrlForThisRequest(string downstreamUrl)
        {
            _requestScopedDataRepository.Add("DownstreamUrl", downstreamUrl);
        }

        public void SetUpstreamRequestForThisRequest(Request.Request request)
        {
            _requestScopedDataRepository.Add("Request", request);
        }
    }
}
