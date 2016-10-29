namespace Ocelot.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class OcelotMiddlewareConfiguration
    { 
        public Func<HttpContext, Func<Task>, Task> PreAuthenticationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> AuthenticationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> PreAuthorisationMiddleware { get; set; }
        public Func<HttpContext, Func<Task>, Task> AuthorisationMiddleware { get; set; }
    }
}