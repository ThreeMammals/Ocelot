using Butterfly.Client.Tracing;
using Ocelot.Configuration.File;
using Ocelot.Requester;

namespace Ocelot.Configuration.Creator
{
    public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
    {
        private readonly IServiceTracer _tracer;

        public HttpHandlerOptionsCreator(IServiceTracer tracer)
        {
            _tracer = tracer;
        }

        public HttpHandlerOptions Create(FileHttpHandlerOptions options)
        {
            var useTracing = _tracer.GetType() != typeof(FakeServiceTracer) && options.UseTracing;

            return new HttpHandlerOptions(options.AllowAutoRedirect,
                options.UseCookieContainer, useTracing);
        }
    }
}
