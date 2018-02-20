using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder;

namespace Ocelot.Middleware
{
    public class DownstreamContext
    {
        public DownstreamContext(HttpContext httpContext)
        {
            this.HttpContext = httpContext;
        }

        public DownstreamRoute DownstreamRoute {get; set;}
        public ServiceProviderConfiguration ServiceProviderConfiguration {get; set;}
        public HttpContext HttpContext { get; private set; }
        public DownstreamReRoute DownstreamReRoute { get; set; }
        public HttpRequestMessage DownstreamRequest { get; set; }
        public HttpResponseMessage DownstreamResponse { get; set; }
        public Request.Request Request { get; set; }
        public Ocelot.Responses.Response<DownstreamContext> Response { get;set; }
        public string RequestId {get;set;}
        public string PreviousRequestId {get;set;}
    }
}
