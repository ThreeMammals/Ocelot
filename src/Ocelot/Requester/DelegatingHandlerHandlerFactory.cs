namespace Ocelot.Requester
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Configuration;
    using Ocelot.Responses;
    using QoS;

    public class DelegatingHandlerHandlerFactory : IDelegatingHandlerHandlerFactory
    {
        private readonly ITracingHandlerFactory _tracingFactory;
        private readonly IQoSFactory _qoSFactory;
        private readonly IServiceProvider _serviceProvider;

        public DelegatingHandlerHandlerFactory(
            ITracingHandlerFactory tracingFactory,
            IQoSFactory qoSFactory,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _tracingFactory = tracingFactory;
            _qoSFactory = qoSFactory;
        }

        public Response<List<Func<DelegatingHandler>>> Get(DownstreamReRoute request)
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
                if (GlobalIsInHandlersConfig(request, handler))
                {
                    reRouteSpecificHandlers.Add(handler.DelegatingHandler);
                }
                else
                {
                    handlers.Add(() => handler.DelegatingHandler);
                }
            }

            if (request.DelegatingHandlers.Any())
            {
                var sorted = SortByConfigOrder(request, reRouteSpecificHandlers);

                foreach (var handler in sorted)
                {
                    handlers.Add(() => handler);
                }
            }

            if (request.HttpHandlerOptions.UseTracing)
            {
                handlers.Add(() => (DelegatingHandler)_tracingFactory.Get());
            }

            if (request.QosOptions.UseQos)
            {
                var handler = _qoSFactory.Get(request);

                if (handler != null && !handler.IsError)
                {
                    handlers.Add(() => handler.Data);
                }
                else
                {
                    return new ErrorResponse<List<Func<DelegatingHandler>>>(handler?.Errors);
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
