using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Ocelot.Requester;

using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public static class OcelotBuilderExtensions
{
    private static readonly Dictionary<Type, Func<Exception, Error>> ErrorMapping = new()
    {
        {typeof(TaskCanceledException), e => new RequestTimedOutError(e)},
        {typeof(TimeoutRejectedException), e => new RequestTimedOutError(e)},
        {typeof(BrokenCircuitException), e => new RequestTimedOutError(e)},
        {typeof(BrokenCircuitException<HttpResponseMessage>), e => new RequestTimedOutError(e)},
    };

    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder,
        QosDelegatingHandlerDelegate delegatingHandler,
        Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
    {
        builder.Services
            .AddSingleton<ResiliencePipelineRegistry<OcelotResiliencePipelineKey>>()
            .AddSingleton(errorMapping)
            .AddSingleton<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>, T>()
            .AddSingleton(delegatingHandler);

        return builder;
    }

    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
        => AddPolly<T>(builder, GetDelegatingHandler, errorMapping);

    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
        => AddPolly<T>(builder, delegatingHandler, ErrorMapping);

    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
        => AddPolly<T>(builder, GetDelegatingHandler, ErrorMapping);

    public static IOcelotBuilder AddPolly(this IOcelotBuilder builder) 
        => AddPolly<PollyQoSResiliencePipelineProvider>(builder, GetDelegatingHandler, ErrorMapping);

    private static DelegatingHandler GetDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyResiliencePipelineDelegatingHandler(route, contextAccessor, loggerFactory);

    #region Obsolete (to remove in a future verison)
    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder,
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

    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder, Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPollyV7<T>(builder, GetDelegatingHandlerV7, errorMapping);

    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPollyV7<T>(builder, delegatingHandler, ErrorMapping);

    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPollyV7<T>(builder, GetDelegatingHandlerV7, ErrorMapping);

    public static IOcelotBuilder AddPollyV7(this IOcelotBuilder builder)
    {
        return AddPollyV7<PollyQoSProvider>(builder, GetDelegatingHandlerV7, ErrorMapping);
    }

    private static DelegatingHandler GetDelegatingHandlerV7(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyPoliciesDelegatingHandler(route, contextAccessor, loggerFactory);

    #endregion
}
