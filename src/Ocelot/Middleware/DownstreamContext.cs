using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Middleware.Multiplexer;
using Ocelot.Request.Middleware;

namespace Ocelot.Middleware
{
    public class DownstreamContext
    {
        public DownstreamContext(HttpContext httpContext)
        {
            HttpContext = httpContext;
            Errors = new List<Error>();
        }

        public List<PlaceholderNameAndValue> TemplatePlaceholderNameAndValues { get; set; }

        public ServiceProviderConfiguration ServiceProviderConfiguration {get; set;}

        public HttpContext HttpContext { get; }

        public DownstreamReRoute DownstreamReRoute { get; set; }

        public DownstreamRequest DownstreamRequest { get; set; }

        public DownstreamResponse DownstreamResponse { get; set; }

        public List<Error> Errors { get; }

        public bool IsError => Errors.Count > 0;
    }
}
