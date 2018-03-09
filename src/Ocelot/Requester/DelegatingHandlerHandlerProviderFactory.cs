using System;
using System.Collections.Generic;
using System.Net.Http;
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
            var handlersAppliedToAll = _serviceProvider.GetServices<DelegatingHandler>();

            var handlers = new List<Func<DelegatingHandler>>();

            foreach (var handler in handlersAppliedToAll)
            {
                handlers.Add(() => handler);
            }

            if (request.HttpHandlerOptions.UseTracing)
            {
                handlers.Add(() => (DelegatingHandler)_factory.Get());
            }

            if (request.IsQos)
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
    }
}
