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
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public static class OcelotBuilderExtensions
{
    public static readonly Dictionary<Type, Func<Exception, Error>> DefaultErrorMapping = new Dictionary<Type, Func<Exception, Error>>
    {
        {typeof(TaskCanceledException), CreateRequestTimedOutError},
        {typeof(TimeoutRejectedException), CreateRequestTimedOutError},
        {typeof(BrokenCircuitException), CreateRequestTimedOutError},
        {typeof(BrokenCircuitException<HttpResponseMessage>), CreateRequestTimedOutError},
    };

    private static Error CreateRequestTimedOutError(Exception e) => new RequestTimedOutError(e);

    /// <summary>
    /// Add Polly QoS provider to Ocelot
    /// </summary>
    /// <typeparam name="T">QOS Provider to use (by default use PollyQoSProvider)</typeparam>
    /// <param name="builder"></param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QOS behavior by yourself)</param>
    /// <param name="errorMapping">Unused</param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder,
            QosDelegatingHandlerDelegate delegatingHandler,
            IDictionary<Type, Func<Exception, Error>> errorMapping)
            where T : class, IPollyQoSProvider<HttpResponseMessage>
    {
        builder.Services
            .AddSingleton(errorMapping)
            .AddSingleton<IPollyQoSProvider<HttpResponseMessage>, T>()
            .AddSingleton(delegatingHandler);

        return builder;
    }

    /// <summary>
    /// Add Polly QoS provider to Ocelot
    /// </summary>
    /// <typeparam name="T">QOS Provider to use (by default use PollyQoSProvider)</typeparam>
    /// <param name="builder"></param>
    /// <param name="errorMapping">Unused</param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, IDictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPolly<T>(builder, GetDelegatingHandler, errorMapping);

    /// <summary>
    /// Add Polly QoS provider to Ocelot with default error mapping
    /// </summary>
    /// <typeparam name="T">QOS Provider to use (by default use PollyQoSProvider)</typeparam>
    /// <param name="builder"></param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QOS behavior by yourself)</param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPolly<T>(builder, delegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Add Polly QoS provider to Ocelot default QOS Delegating Handler
    /// </summary>
    /// <typeparam name="T">QOS Provider to use with default QOS Provider</typeparam>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPolly<T>(builder, GetDelegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Add Polly QoS provider to Ocelot with default QOS Provider and default QOS Delegating Handler
    /// </summary>
   /// <param name="builder"></param>
    /// <returns></returns>
    public static IOcelotBuilder AddPolly(this IOcelotBuilder builder)
    {
        return AddPolly<PollyQoSProvider>(builder, GetDelegatingHandler, DefaultErrorMapping);
    }

    private static DelegatingHandler GetDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyPoliciesDelegatingHandler(route, contextAccessor, loggerFactory);
}
