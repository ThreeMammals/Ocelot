using Ocelot.Middleware.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ocelot.Security.Middleware
{
    public static class SecurityMiddlewareExtensions
    {
        public static IOcelotPipelineBuilder UseSecurityMiddleware(this IOcelotPipelineBuilder builder)
        {
            return builder.UseMiddleware<SecurityMiddleware>();
        }
    }
}
