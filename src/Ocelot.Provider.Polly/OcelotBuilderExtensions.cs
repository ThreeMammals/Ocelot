using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Ocelot.Requester;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder,
        QosDelegatingHandlerDelegate delegatingHandler,
        Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSProvider<HttpResponseMessage>
    {
        builder.Services
            .AddSingleton(errorMapping)
            .AddSingleton<IPollyQoSProvider<HttpResponseMessage>, T>()
            .AddSingleton(delegatingHandler);

        return builder;
    }

    public static IOcelotBuilder AddPolly(this IOcelotBuilder builder)
    {
        var errorMapping = new Dictionary<Type, Func<Exception, Error>>
        {
            { typeof(TaskCanceledException), e => new RequestTimedOutError(e) },
            { typeof(TimeoutRejectedException), e => new RequestTimedOutError(e) },
            { typeof(BrokenCircuitException), e => new RequestTimedOutError(e) },
            { typeof(BrokenCircuitException<HttpResponseMessage>), e => new RequestTimedOutError(e) },
        };
        return AddPolly<PollyQoSProvider>(builder, GetDelegatingHandler, errorMapping);
    }

    private static DelegatingHandler GetDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyPoliciesDelegatingHandler(route, contextAccessor, loggerFactory);
}
