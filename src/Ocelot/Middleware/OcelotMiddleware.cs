using System.Collections.Generic;
using System.Net.Http;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Errors;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Values;

namespace Ocelot.Middleware
{
    public abstract class OcelotMiddleware
    {
        private readonly IRequestScopedDataRepository _requestScopedDataRepository;

        protected OcelotMiddleware(IRequestScopedDataRepository requestScopedDataRepository)
        {
            _requestScopedDataRepository = requestScopedDataRepository;
        }

        public bool PipelineError
        {
            get
            {
                var response = _requestScopedDataRepository.Get<bool>("OcelotMiddlewareError");
                return response.Data;
            }
        }

        public List<Error> PipelineErrors
        {
            get
            {
                var response = _requestScopedDataRepository.Get<List<Error>>("OcelotMiddlewareErrors");
                return response.Data;
            }
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

        public HttpResponseMessage HttpResponseMessage
        {
            get
            {
                var request = _requestScopedDataRepository.Get<HttpResponseMessage>("HttpResponseMessage");
                return request.Data;
            }
        }

        public HostAndPort HostAndPort 
        {
            get
            {
                var hostAndPort = _requestScopedDataRepository.Get<HostAndPort>("HostAndPort");
                return hostAndPort.Data;
            }
        }

        public void SetHostAndPortForThisRequest(HostAndPort hostAndPort)
        {
            _requestScopedDataRepository.Add("HostAndPort", hostAndPort);
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

        public void SetHttpResponseMessageThisRequest(HttpResponseMessage responseMessage)
        {
            _requestScopedDataRepository.Add("HttpResponseMessage", responseMessage);
        }

        public void SetPipelineError(List<Error> errors)
        {
            _requestScopedDataRepository.Add("OcelotMiddlewareError", true);
            _requestScopedDataRepository.Add("OcelotMiddlewareErrors", errors);
        }
    }
}
