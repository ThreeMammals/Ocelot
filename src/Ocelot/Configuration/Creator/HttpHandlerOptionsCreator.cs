using Butterfly.Client.Tracing;
using Ocelot.Configuration.File;
using Ocelot.Requester;

namespace Ocelot.Configuration.Creator
{
    public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
    {
        private IServiceTracer _tracer;

        public HttpHandlerOptionsCreator(IServiceTracer tracer)
        {
            _tracer = tracer;
        }

        public HttpHandlerOptions Create(FileReRoute fileReRoute)
        {
            var useTracing = _tracer.GetType() != typeof(FakeServiceTracer) ? fileReRoute.HttpHandlerOptions.UseTracing : false;

            return new HttpHandlerOptions(fileReRoute.HttpHandlerOptions.AllowAutoRedirect,
                fileReRoute.HttpHandlerOptions.UseCookieContainer, useTracing);
        }
    }
}
