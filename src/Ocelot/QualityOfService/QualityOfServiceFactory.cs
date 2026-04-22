using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
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

        var external = _serviceProvider.GetService<QosDelegatingHandlerDelegate>();
        if (external != null)
        {
            _logger.LogInformation(() => $"Ocelot identified that an external Quality of Service lib aka {external.GetType().Namespace} registered. Going to invoke {external.GetType().Name} for route -> {route.Name()} ...");
            var handler = external.Invoke(route, _contextAccessor, _loggerFactory);
            if (handler != null)
                return handler;

            var error = new UnableToFindQoSProviderError($"Fatal error when creating delegating handler instance by {external.GetType().Name} for route -> {route.Name()}");
            _contextAccessor.HttpContext.Items.SetError(error); // TODO Better exception to be caught by MessageInvokerHttpRequester?
            _logger.LogCritical(error.ToString, null);
            return new NoQosDelegatingHandler();
        }

        // Built-in Quality of Service feature
        if (opts.MinimumThroughput.HasValue || opts.BreakDuration.HasValue)
            return new CircuitBreakerDelegatingHandler(route, _loggerFactory);

        // TODO Timeout strategy, but it requires refactoring of MessageInvokerPool and TimeoutDelegatingHandler
        //if (opts.Timeout.HasValue)
        //    return new TimeoutDelegatingHandler(TimeSpan.FromMilliseconds(opts.Timeout.Value));
        return new NoQosDelegatingHandler();
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
