using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Errors.QoS;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Ocelot.Requester;

using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public static class OcelotBuilderExtensions
{
    public static readonly Dictionary<Type, Func<Exception, Error>> DefaultErrorMapping = new()
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

    /// <summary>
    /// Add Polly QoS provider to Ocelot
    /// </summary>
    /// <typeparam name="T">QOS Provider to use (by default use PollyQoSResiliencePipelineProvider)</typeparam>
    /// <param name="builder"></param>
    /// <param name="errorMapping">Unused</param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
        => AddPolly<T>(builder, GetDelegatingHandler, errorMapping);

    /// <summary>
    /// Add Polly QoS provider to Ocelot
    /// </summary>
    /// <typeparam name="T">QOS Provider to use (by default use PollyQoSResiliencePipelineProvider)</typeparam>
    /// <param name="builder"></param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QOS behavior by yourself)</param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
        => AddPolly<T>(builder, delegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Add Polly QoS provider to Ocelot
    /// </summary>
    /// <typeparam name="T">QOS Provider to use (by default use PollyQoSResiliencePipelineProvider)</typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder)
        where T : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage> 
        => AddPolly<T>(builder, GetDelegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Add Polly QoS provider to Ocelot
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly(this IOcelotBuilder builder) 
        => AddPolly<PollyQoSResiliencePipelineProvider>(builder, GetDelegatingHandler, DefaultErrorMapping);

    private static DelegatingHandler GetDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyResiliencePipelineDelegatingHandler(route, contextAccessor, loggerFactory);

    #region Obsolete (to remove in a future verison)

    [Obsolete("Use AddPolly instead, it will be remove in future version")]
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

    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder, Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPollyV7<T>(builder, GetDelegatingHandlerV7, errorMapping);

    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPollyV7<T>(builder, delegatingHandler, DefaultErrorMapping);

    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<T>(this IOcelotBuilder builder)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPollyV7<T>(builder, GetDelegatingHandlerV7, DefaultErrorMapping);

    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7(this IOcelotBuilder builder)
    {
        return AddPollyV7<PollyQoSProvider>(builder, GetDelegatingHandlerV7, DefaultErrorMapping);
    }

    private static DelegatingHandler GetDelegatingHandlerV7(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyPoliciesDelegatingHandler(route, contextAccessor, loggerFactory);

    #endregion
}
