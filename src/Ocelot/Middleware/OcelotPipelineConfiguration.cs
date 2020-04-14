namespace Ocelot.Middleware
{
    using Ocelot.Middleware.Pipeline;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    public class OcelotPipelineConfiguration
    {
        /// <summary>
        /// This is called after the global error handling middleware so any code before calling next.invoke
        /// is the next thing called in the Ocelot pipeline. Anything after next.invoke is the last thing called
        /// in the Ocelot pipeline before we go to the global error handler.
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> PreErrorResponderMiddleware { get; set; }

        /// <summary>
        /// This is to allow the user to run any extra authentication before the Ocelot authentication
        /// kicks in
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> PreAuthenticationMiddleware { get; set; }

        /// <summary>
        /// This allows the user to completely override the ocelot authentication middleware
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> AuthenticationMiddleware { get; set; }

        /// <summary>
        /// This is to allow the user to run any extra authorisation before the Ocelot authentication
        /// kicks in
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> PreAuthorisationMiddleware { get; set; }

        /// <summary>
        /// This allows the user to completely override the ocelot authorisation middleware
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> AuthorisationMiddleware { get; set; }

        /// <summary>
        /// This allows the user to implement there own query string manipulation logic
        /// </summary>
        public Func<RequestDelegate, RequestDelegate> PreQueryStringBuilderMiddleware { get; set; }

        /// <summary>
        /// This is an extension that will branch to different pipes
        /// </summary>
        // todo fix this data structure
        public Dictionary<Func<HttpContext,  bool>, Action<IApplicationBuilder>> MapWhenOcelotPipeline { get; } = new Dictionary<Func<HttpContext, bool>, Action<IApplicationBuilder>>();
    }
}
