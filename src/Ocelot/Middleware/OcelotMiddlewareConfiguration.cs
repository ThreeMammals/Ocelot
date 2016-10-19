namespace Ocelot.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class OcelotMiddlewareConfiguration
    {
        public Func<HttpContext, Func<Task>, Task> PreHttpResponderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostHttpResponderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreDownstreamRouteFinderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostDownstreamRouteFinderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreAuthenticationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostAuthenticationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreClaimsBuilderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostClaimsBuilderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreAuthorisationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostAuthorisationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreHttpRequestHeadersBuilderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostHttpRequestHeadersBuilderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreDownstreamUrlCreatorMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostDownstreamUrlCreatorMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreHttpRequestBuilderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PostHttpRequestBuilderMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreHttpRequesterMiddleware { get; set; }
    }
}