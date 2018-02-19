using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ocelot.DownstreamRouteFinder.Middleware;

namespace Ocelot.RateLimit.Middleware
{
    public interface IOcelotApplicationBuilder
    {
        IApplicationBuilder Use(Func<OcelotRequestDelegate, OcelotRequestDelegate> middleware);
    }

    public class OcelotApplicationBuilder : IOcelotApplicationBuilder
    {
        public IApplicationBuilder Use(Func<OcelotRequestDelegate, OcelotRequestDelegate> middleware)
        {
            throw new NotImplementedException();
        }
    }

    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientRateLimitMiddleware>();
        }
    }
}
