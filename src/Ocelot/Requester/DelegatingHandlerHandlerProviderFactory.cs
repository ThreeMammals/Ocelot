using Ocelot.Logging;

namespace Ocelot.Requester
{
    public class DelegatingHandlerHandlerProviderFactory : IDelegatingHandlerHandlerProviderFactory
    {
        private readonly IOcelotLoggerFactory _loggerFactory;
        private readonly IDelegatingHandlerHandlerProvider _allRoutesProvider;

        public DelegatingHandlerHandlerProviderFactory(IOcelotLoggerFactory loggerFactory, IDelegatingHandlerHandlerProvider allRoutesProvider)
        {
            _loggerFactory = loggerFactory;
            _allRoutesProvider = allRoutesProvider;
        }

        public IDelegatingHandlerHandlerProvider Get(Request.Request request)
        {
            var handlersAppliedToAll = _allRoutesProvider.Get();

            var provider = new DelegatingHandlerHandlerProvider();

            foreach (var handler in handlersAppliedToAll)
            {
                provider.Add(handler);
            }

            if (request.IsQos)
            {
                provider.Add(() => new PollyCircuitBreakingDelegatingHandler(request.QosProvider, _loggerFactory));
            }

            return provider;
        }
    }
}
