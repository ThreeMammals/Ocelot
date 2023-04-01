﻿using Microsoft.AspNetCore.Builder;
using Ocelot.Middleware;

namespace Ocelot.Errors.Middleware
{
    public static class ExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.TryUseOcelotMiddleware<IOcelotExceptionHandlerMiddleware, ExceptionHandlerMiddleware>();
        }
    }
}
