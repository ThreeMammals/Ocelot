using System.Net.Http;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Requester.QoS;
using Ocelot.Responses;

namespace Ocelot.Requester
{
    public class DelegatingHandlerHandlerProviderFactory : IDelegatingHandlerHandlerProviderFactory
    {
        private readonly ITracingHandler _tracingHandler;
        private readonly IOcelotLoggerFactory _loggerFactory;
        private readonly IDelegatingHandlerHandlerProvider _allRoutesProvider;
        private readonly IQosProviderHouse _qosProviderHouse;

        public DelegatingHandlerHandlerProviderFactory(IOcelotLoggerFactory loggerFactory, 
            IDelegatingHandlerHandlerProvider allRoutesProvider, 
            ITracingHandler tracingHandler,
            IQosProviderHouse qosProviderHouse)
        {
            _tracingHandler = tracingHandler;
            _loggerFactory = loggerFactory;
            _allRoutesProvider = allRoutesProvider;
            _qosProviderHouse = qosProviderHouse;
        }

        public Response<IDelegatingHandlerHandlerProvider> Get(DownstreamReRoute request)
        {
            var handlersAppliedToAll = _allRoutesProvider.Get();

            var provider = new DelegatingHandlerHandlerProvider();

            foreach (var handler in handlersAppliedToAll)
            {
                provider.Add(handler);
            }

            if (request.HttpHandlerOptions.UseTracing)
            {
                provider.Add(() => (DelegatingHandler)_tracingHandler);
            }

            if (request.IsQos)
            {
                var qosProvider = _qosProviderHouse.Get(request);

                if (qosProvider.IsError)
                {
                    return new ErrorResponse<IDelegatingHandlerHandlerProvider>(qosProvider.Errors);
                }

                provider.Add(() => new PollyCircuitBreakingDelegatingHandler(qosProvider.Data, _loggerFactory));
            }

            return new OkResponse<IDelegatingHandlerHandlerProvider>(provider);
        }
    }
}
