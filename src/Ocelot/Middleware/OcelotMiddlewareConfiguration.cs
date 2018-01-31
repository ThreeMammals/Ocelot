namespace Ocelot.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class OcelotMiddlewareConfiguration
    {
        /// <summary>
        /// This is called after the global error handling middleware so any code before calling next.invoke 
        /// is the next thing called in the Ocelot pipeline. Anything after next.invoke is the last thing called
        /// in the Ocelot pipeline before we go to the global error handler.
        /// </summary>
        public Func<HttpContext, Func<Task>, Task> PreErrorResponderMiddleware { get; set; }

        /// <summary>
        /// This is to allow the user to run any extra authentication before the Ocelot authentication
        /// kicks in
        /// </summary>
        public Func<HttpContext, Func<Task>, Task> PreAuthenticationMiddleware { get; set; }

        /// <summary>
        /// This allows the user to completely override the ocelot authentication middleware
        /// </summary>
        public Func<HttpContext, Func<Task>, Task> AuthenticationMiddleware { get; set; }

        /// <summary>
        /// This is to allow the user to run any extra authorisation before the Ocelot authentication
        /// kicks in
        /// </summary>
        public Func<HttpContext, Func<Task>, Task> PreAuthorisationMiddleware { get; set; }

        /// <summary>
        /// This allows the user to completely override the ocelot authorisation middleware
        /// </summary>
        public Func<HttpContext, Func<Task>, Task> AuthorisationMiddleware { get; set; }

        /// <summary>
        /// This allows the user to implement there own query string manipulation logic
        /// </summary>
        public Func<HttpContext, Func<Task>, Task> PreQueryStringBuilderMiddleware { get; set; }
        
    }
}