using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;

namespace Ocelot.Middleware
{
    public class DownstreamContext
    {
        public DownstreamContext(HttpContext httpContext)
        {
            this.HttpContext = httpContext;
            Errors = new List<Error>();
        }

        public List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; set; }
        public ServiceProviderConfiguration ServiceProviderConfiguration {get; set;}
        public HttpContext HttpContext { get; private set; }
        public DownstreamReRoute DownstreamReRoute { get; set; }
        public HttpRequestMessage DownstreamRequest { get; set; }
        public HttpResponseMessage DownstreamResponse { get; set; }
        public List<Error> Errors { get;set; }
        public bool IsError => Errors.Count > 0;
    }
}
