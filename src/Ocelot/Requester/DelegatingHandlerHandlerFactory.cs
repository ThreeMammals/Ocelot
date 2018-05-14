using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Butterfly.Client.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public class DelegatingHandlerHandlerFactory : IDelegatingHandlerHandlerFactory
    {
        private readonly ITracingHandlerFactory _factory;
        private readonly IOcelotLoggerFactory _loggerFactory;
        private readonly IQosProviderHouse _qosProviderHouse;
        private readonly IServiceProvider _serviceProvider;

        public DelegatingHandlerHandlerFactory(IOcelotLoggerFactory loggerFactory, 
            ITracingHandlerFactory factory,
            IQosProviderHouse qosProviderHouse,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _factory = factory;
            _loggerFactory = loggerFactory;
            _qosProviderHouse = qosProviderHouse;
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
                handlers.Add(() => (DelegatingHandler)_factory.Get());
            }

            if (request.QosOptions.UseQos)
            {
                var qosProvider = _qosProviderHouse.Get(request);

                if (qosProvider.IsError)
                {
                    return new ErrorResponse<List<Func<DelegatingHandler>>>(qosProvider.Errors);
                }

                handlers.Add(() => new PollyCircuitBreakingDelegatingHandler(qosProvider.Data, _loggerFactory));
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
