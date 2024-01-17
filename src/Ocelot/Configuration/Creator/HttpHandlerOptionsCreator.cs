using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Logging;

namespace Ocelot.Configuration.Creator
{
    public class HttpHandlerOptionsCreator : IHttpHandlerOptionsCreator
    {
        private readonly ITracer _tracer;

        //todo: this should be configurable and available as global config parameter in ocelot.json
        public const int DefaultPooledConnectionLifetimeSeconds = 120;

        public HttpHandlerOptionsCreator(IServiceProvider services)
        {
            _tracer = services.GetService<ITracer>();
        }

        public HttpHandlerOptions Create(FileHttpHandlerOptions options)
        {
            var useTracing = _tracer != null && options.UseTracing;

            //be sure that maxConnectionPerServer is in correct range of values
            var maxConnectionPerServer = (options.MaxConnectionsPerServer > 0) ? options.MaxConnectionsPerServer : int.MaxValue;
            var pooledConnectionLifetime = TimeSpan.FromSeconds(options.PooledConnectionLifetimeSeconds ?? DefaultPooledConnectionLifetimeSeconds);

            return new HttpHandlerOptions(options.AllowAutoRedirect,
                options.UseCookieContainer, useTracing, options.UseProxy, maxConnectionPerServer, pooledConnectionLifetime);
        }
    }
}
