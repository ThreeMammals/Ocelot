namespace Ocelot.Requester
{
    using Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Configuration;
    using Ocelot.Responses;
    using QoS;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;

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

        public Response<List<Func<DelegatingHandler>>> Get(DownstreamReRoute downstreamReRoute)
        {
            var globalDelegatingHandlers = _serviceProvider
                .GetServices<GlobalDelegatingHandler>()
                .ToList();

            var reRouteSpecificHandlers = _serviceProvider
                .GetServices<DelegatingHandler>()
                .ToList();

            var handlers = new List<Func<DelegatingHandler>>();

            foreach (var handler in globalDelegatingHandlers)
            {
                if (GlobalIsInHandlersConfig(downstreamReRoute, handler))
                {
                    reRouteSpecificHandlers.Add(handler.DelegatingHandler);
                }
                else
                {
                    handlers.Add(() => handler.DelegatingHandler);
                }
            }

            if (downstreamReRoute.DelegatingHandlers.Any())
            {
                var sorted = SortByConfigOrder(downstreamReRoute, reRouteSpecificHandlers);

                foreach (var handler in sorted)
                {
                    handlers.Add(() => handler);
                }
            }

            if (downstreamReRoute.HttpHandlerOptions.UseTracing)
            {
                handlers.Add(() => (DelegatingHandler)_tracingFactory.Get());
            }

            if (downstreamReRoute.QosOptions.UseQos)
            {
                var handler = _qoSFactory.Get(downstreamReRoute);

                if (handler != null && !handler.IsError)
                {
                    handlers.Add(() => handler.Data);
                }
                else
                {
                    _logger.LogWarning($"ReRoute {downstreamReRoute.UpstreamPathTemplate} specifies use QoS but no QosHandler found in DI container. Will use not use a QosHandler, please check your setup!");
                    handlers.Add(() => new NoQosDelegatingHandler());
                }
            }

            return new OkResponse<List<Func<DelegatingHandler>>>(handlers);
        }

        private List<DelegatingHandler> SortByConfigOrder(DownstreamReRoute request, List<DelegatingHandler> reRouteSpecificHandlers)
        {
            return reRouteSpecificHandlers
                .Where(x => request.DelegatingHandlers.Contains(x.GetType().Name))
                .OrderBy(d =>
                {
                    var type = d.GetType().Name;
                    var pos = request.DelegatingHandlers.IndexOf(type);
                    return pos;
                }).ToList();
        }

        private bool GlobalIsInHandlersConfig(DownstreamReRoute request, GlobalDelegatingHandler handler)
        {
            return request.DelegatingHandlers.Contains(handler.DelegatingHandler.GetType().Name);
        }
    }
}
