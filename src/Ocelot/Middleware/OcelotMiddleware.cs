using System.Collections.Generic;
using System.Net.Http;
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
            MiddlwareName = this.GetType().Name;
        }

        public string MiddlwareName { get; }

        public bool PipelineError => _requestScopedDataRepository.Get<bool>("OcelotMiddlewareError").Data;

        public List<Error> PipelineErrors => _requestScopedDataRepository.Get<List<Error>>("OcelotMiddlewareErrors").Data;

        public DownstreamRoute DownstreamRoute => _requestScopedDataRepository.Get<DownstreamRoute>("DownstreamRoute").Data;

        public Request.Request Request => _requestScopedDataRepository.Get<Request.Request>("Request").Data;

        public HttpRequestMessage DownstreamRequest => _requestScopedDataRepository.Get<HttpRequestMessage>("DownstreamRequest").Data;

        public HttpResponseMessage HttpResponseMessage => _requestScopedDataRepository.Get<HttpResponseMessage>("HttpResponseMessage").Data;

        public void SetDownstreamRouteForThisRequest(DownstreamRoute downstreamRoute)
        {
            _requestScopedDataRepository.Add("DownstreamRoute", downstreamRoute);
        }

        public void SetUpstreamRequestForThisRequest(Request.Request request)
        {
            _requestScopedDataRepository.Add("Request", request);
        }

        public void SetDownstreamRequest(HttpRequestMessage request)
        {
            _requestScopedDataRepository.Add("DownstreamRequest", request);
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
