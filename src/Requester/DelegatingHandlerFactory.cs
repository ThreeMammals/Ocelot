using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.QualityOfService;

namespace Ocelot.Requester;

public class DelegatingHandlerFactory : IDelegatingHandlerFactory
{
    private readonly ITracingHandlerFactory _tracingFactory;
    private readonly IQualityOfServiceFactory _qosFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOcelotLogger _logger;

    public DelegatingHandlerFactory(
        ITracingHandlerFactory tracingFactory,
        IQualityOfServiceFactory qosFactory,
        IServiceProvider serviceProvider,
        IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DelegatingHandlerFactory>();
        _serviceProvider = serviceProvider;
        _tracingFactory = tracingFactory;
        _qosFactory = qosFactory;
    }

    public List<DelegatingHandler> Get(DownstreamRoute route)
    {
        var globalDelegatingHandlers = _serviceProvider.GetServices<GlobalDelegatingHandler>()
            .ToArray();
        var routeSpecificHandlers = _serviceProvider.GetServices<DelegatingHandler>()
            .ToList();
        var handlers = new List<DelegatingHandler>();

        foreach (var handler in globalDelegatingHandlers)
        {
            if (GlobalIsInHandlersConfig(route, handler))
            {
                routeSpecificHandlers.Add(handler.DelegatingHandler);
            }
            else
            {
                handlers.Add(handler.DelegatingHandler);
            }
        }

        if (route.DelegatingHandlers.Count != 0)
        {
            var sorted = SortByConfigOrder(route, routeSpecificHandlers);
            handlers.AddRange(sorted);
        }

        if (route.HttpHandlerOptions.UseTracing)
        {
            var tracing = (DelegatingHandler)_tracingFactory.Get();
            handlers.Add(tracing);
        }

        if (route.QosOptions.UseQos)
        {
            var qos = _qosFactory.Get(route);
            handlers.Add(qos);
        }

        return handlers;
    }

    private static DelegatingHandler[] SortByConfigOrder(DownstreamRoute request, List<DelegatingHandler> routeSpecificHandlers)
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
