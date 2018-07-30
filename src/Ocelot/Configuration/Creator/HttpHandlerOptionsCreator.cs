namespace Ocelot.Configuration.Creator
{
    using System;
    using Butterfly.Client.Tracing;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Configuration.File;
    using Ocelot.Requester;

    public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
    {
        private readonly IServiceTracer _tracer;

        public HttpHandlerOptionsCreator(IServiceProvider services)
        {
            _tracer = services.GetService<IServiceTracer>();
        }

        public HttpHandlerOptions Create(FileHttpHandlerOptions options)
        {
            var useTracing = _tracer!= null && options.UseTracing;

            return new HttpHandlerOptions(options.AllowAutoRedirect,
                options.UseCookieContainer, useTracing, options.UseProxy);
        }
    }
}
