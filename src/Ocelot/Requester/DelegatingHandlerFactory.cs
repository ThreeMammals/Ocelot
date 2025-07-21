using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester;

public class DelegatingHandlerFactory : IDelegatingHandlerFactory
{
    private readonly ITracingHandlerFactory _tracingFactory;
    private readonly IQoSFactory _qoSFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOcelotLogger _logger;

    public DelegatingHandlerFactory(
        ITracingHandlerFactory tracingFactory,
        IQoSFactory qoSFactory,
        IServiceProvider serviceProvider,
        IOcelotLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DelegatingHandlerFactory>();
        _serviceProvider = serviceProvider;
        _tracingFactory = tracingFactory;
        _qoSFactory = qoSFactory;
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
            handlers.Add((DelegatingHandler)_tracingFactory.Get());
        }

        if (route.QosOptions.UseQos)
        {
            var handler = _qoSFactory.Get(route);
            if (handler?.IsError == false)
            {
                handlers.Add(handler.Data);
            }
            else
            {
                _logger.LogWarning(() => $"Route '{route.Name()}' specifies use QoS but no QosHandler found in DI container. Will use not use a QosHandler, please check your setup!");
                handlers.Add(new NoQosDelegatingHandler());
            }
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
