using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public class DelegatingHandlerHandlerFactory : IDelegatingHandlerHandlerFactory
    {
        private readonly ITracingHandlerFactory _tracingFactory;
        private readonly IQoSFactory _qoSFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOcelotLogger _logger;

        public DelegatingHandlerHandlerFactory(
            ITracingHandlerFactory tracingFactory,
            IQoSFactory qoSFactory,
            IServiceProvider serviceProvider,
            IOcelotLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DelegatingHandlerHandlerFactory>();
            _serviceProvider = serviceProvider;
            _tracingFactory = tracingFactory;
            _qoSFactory = qoSFactory;
        }

        public Response<List<Func<DelegatingHandler>>> Get(DownstreamRoute downstreamRoute)
        {
            var globalDelegatingHandlers = _serviceProvider
                .GetServices<GlobalDelegatingHandler>()
                .ToArray();

            var routeSpecificHandlers = _serviceProvider
                .GetServices<DelegatingHandler>()
                .ToList();

            var handlers = new List<Func<DelegatingHandler>>();

            foreach (var handler in globalDelegatingHandlers)
            {
                if (GlobalIsInHandlersConfig(downstreamRoute, handler))
                {
                    routeSpecificHandlers.Add(handler.DelegatingHandler);
                }
                else
                {
                    handlers.Add(() => handler.DelegatingHandler);
                }
            }

            if (downstreamRoute.DelegatingHandlers.Any())
            {
                var sorted = SortByConfigOrder(downstreamRoute, routeSpecificHandlers);

                handlers.AddRange(sorted.Select(handler => (Func<DelegatingHandler>)(() => handler)));
            }

            if (downstreamRoute.HttpHandlerOptions.UseTracing)
            {
                handlers.Add(() => (DelegatingHandler)_tracingFactory.Get());
            }

            if (downstreamRoute.QosOptions.UseQos)
            {
                var handler = _qoSFactory.Get(downstreamRoute);

                if (handler?.IsError == false)
                {
                    handlers.Add(() => handler.Data);
                }
                else
                {
                    _logger.LogWarning(() => $"Route {downstreamRoute.UpstreamPathTemplate} specifies use QoS but no QosHandler found in DI container. Will use not use a QosHandler, please check your setup!");
                    handlers.Add(() => new NoQosDelegatingHandler());
                }
            }

            return new OkResponse<List<Func<DelegatingHandler>>>(handlers);
        }

        private static IEnumerable<DelegatingHandler> SortByConfigOrder(DownstreamRoute request, IEnumerable<DelegatingHandler> routeSpecificHandlers)
        {
            return routeSpecificHandlers
                .Where(x => request.DelegatingHandlers.Contains(x.GetType().Name))
                .OrderBy(d =>
                {
                    var type = d.GetType().Name;
                    var pos = request.DelegatingHandlers.IndexOf(type);
                    return pos;
                }).ToArray();
        }

        private static bool GlobalIsInHandlersConfig(DownstreamRoute request, GlobalDelegatingHandler handler) =>
            request.DelegatingHandlers.Contains(handler.DelegatingHandler.GetType().Name);
    }
}
