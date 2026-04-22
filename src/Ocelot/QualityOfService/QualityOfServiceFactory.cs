using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace Ocelot.QualityOfService;

public class QualityOfServiceFactory : IQualityOfServiceFactory, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOcelotLoggerFactory _loggerFactory;
    private readonly IHttpContextAccessor _contextAccessor;
    private IOcelotLogger _logger;

    public QualityOfServiceFactory(IServiceProvider serviceProvider, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _contextAccessor = contextAccessor;
        _logger = loggerFactory.CreateLogger<QualityOfServiceFactory>();
    }

    public DelegatingHandler Get(DownstreamRoute route)
    {
        var opts = route.QosOptions;
        if (!opts.UseQos)
            return new NoQosDelegatingHandler();

        var finder = _serviceProvider.GetService<QosDelegatingHandlerDelegate>();
        if (finder is null)
        {
            // This duplicates FileQoSOptionsFluentValidator but added for consistency
            var err = new UnableToFindQoSProviderError($"Could not find {nameof(QosDelegatingHandlerDelegate)}! Either a {nameof(Route)} or {nameof(FileConfiguration.GlobalConfiguration)} are using {nameof(FileRoute.QoSOptions)} but no {nameof(QosDelegatingHandlerDelegate)} has been registered in dependency injection container.");
            _contextAccessor.HttpContext?.Items.SetError(err);
            _logger.LogCritical(err.ToString, null);
            return new NoQosDelegatingHandler();
        }

#if DEBUG
        _logger.LogDebug(() => $"Ocelot identified that an external Quality of Service lib aka {finder.GetType().Namespace} registered. Going to invoke {finder.GetType().Name} for route -> {route.Name()} ...");
#endif
        var handler = finder.Invoke(route, _contextAccessor, _loggerFactory);
        if (handler is null)
        {
            var error = new UnableToFindQoSProviderError($"Fatal error when creating delegating handler instance by {finder.GetType().Name} for route -> {route.Name()}");
            _contextAccessor.HttpContext?.Items.SetError(error); // TODO Better exception to be caught by MessageInvokerHttpRequester?
            _logger.LogCritical(error.ToString, null);
            return new NoQosDelegatingHandler();
        }

        if (handler is not QosDelegatingHandler)
            return handler; // this is an external QoS implementation

        // Built-in Quality of Service: return CircuitBreakerDelegatingHandler for any active QoS option
        return new CircuitBreakerDelegatingHandler(route, _loggerFactory);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger?.Dispose();
            _logger = null;
        }
    }
}
