using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Errors.QoS;
using Ocelot.Logging;
using Ocelot.Provider.Polly.Interfaces;
using Ocelot.Provider.Polly.v7;
using Ocelot.Requester;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Ocelot.Provider.Polly;

public static class OcelotBuilderExtensions
{
    /// <summary>
    /// Default mapping of Polly <see cref="Exception"/>s to <see cref="Error"/> objects.
    /// </summary>
    public static readonly IDictionary<Type, Func<Exception, Error>> DefaultErrorMapping = new Dictionary<Type, Func<Exception, Error>>
    {
        {typeof(TaskCanceledException), CreateRequestTimedOutError},
        {typeof(TimeoutRejectedException), CreateRequestTimedOutError},
        {typeof(BrokenCircuitException), CreateRequestTimedOutError},
        {typeof(BrokenCircuitException<HttpResponseMessage>), CreateRequestTimedOutError},
    };

    private static Error CreateRequestTimedOutError(Exception e) => new RequestTimedOutError(e);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by custom delegate and with custom error mapping.
    /// </summary>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QoS behavior by yourself).</param>
    /// <param name="errorMapping">Your customized error mapping.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<TProvider>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler, IDictionary<Type, Func<Exception, Error>> errorMapping)
        where TProvider : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
    {
        builder.Services
            .AddSingleton<ResiliencePipelineRegistry<OcelotResiliencePipelineKey>>()
            .AddSingleton(errorMapping) // Dictionary<TKey, TValue> injection used in HttpExceptionToErrorMapper
            .AddSingleton<IPollyQoSResiliencePipelineProvider<HttpResponseMessage>, TProvider>()
            .AddSingleton(delegatingHandler);
        return builder;
    }

    /// <summary>
    /// Adds Polly QoS provider to Ocelot with custom error mapping, but default <see cref="DelegatingHandler"/> is used.
    /// </summary>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSResiliencePipelineProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="errorMapping">Your customized error mapping.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<TProvider>(this IOcelotBuilder builder, IDictionary<Type, Func<Exception, Error>> errorMapping)
        where TProvider : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
        => AddPolly<TProvider>(builder, GetDelegatingHandler, errorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot with custom <see cref="DelegatingHandler"/> delegate, but default error mapping is used.
    /// </summary>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSResiliencePipelineProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QoS behavior by yourself).</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<TProvider>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where TProvider : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
        => AddPolly<TProvider>(builder, delegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by defaults.
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <list type="bullet">
    ///   <item><see cref="GetDelegatingHandler"/></item>
    ///   <item><see cref="DefaultErrorMapping"/></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSResiliencePipelineProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<TProvider>(this IOcelotBuilder builder)
        where TProvider : class, IPollyQoSResiliencePipelineProvider<HttpResponseMessage>
        => AddPolly<TProvider>(builder, GetDelegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by defaults with default QoS provider.
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <list type="bullet">
    ///   <item><see cref="PollyQoSResiliencePipelineProvider"/></item>
    ///   <item><see cref="GetDelegatingHandlerV7"/></item>
    ///   <item><see cref="DefaultErrorMapping"/></item>
    /// </list>
    /// </remarks>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly(this IOcelotBuilder builder)
        => AddPolly<PollyQoSResiliencePipelineProvider>(builder, GetDelegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Creates default delegating handler based on the <see cref="PollyResiliencePipelineDelegatingHandler"/> type.
    /// </summary>
    /// <param name="route">The downstream route to apply the handler for.</param>
    /// <param name="contextAccessor">The context accessor of the route.</param>
    /// <param name="loggerFactory">The factory of logger.</param>
    /// <returns>A <see cref="DelegatingHandler"/> object, but concrete type is the <see cref="PollyResiliencePipelineDelegatingHandler"/> class.</returns>
    private static DelegatingHandler GetDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyResiliencePipelineDelegatingHandler(route, contextAccessor, loggerFactory);

    #region Obsolete extensions will be removed in future version

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by custom delegate and with custom error mapping.
    /// </summary>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QoS behavior by yourself).</param>
    /// <param name="errorMapping">Your customized error mapping.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<TProvider>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler, IDictionary<Type, Func<Exception, Error>> errorMapping)
        where TProvider : class, IPollyQoSProvider<HttpResponseMessage>
    {
        builder.Services
            .AddSingleton(errorMapping)
            .AddSingleton<IPollyQoSProvider<HttpResponseMessage>, TProvider>()
            .AddSingleton(delegatingHandler);
        return builder;
    }

    /// <summary>
    /// Adds Polly QoS provider to Ocelot with custom error mapping, but default <see cref="DelegatingHandler"/> is used.
    /// </summary>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="errorMapping">Your customized error mapping.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<TProvider>(this IOcelotBuilder builder, IDictionary<Type, Func<Exception, Error>> errorMapping)
        where TProvider : class, IPollyQoSProvider<HttpResponseMessage>
        => AddPollyV7<TProvider>(builder, GetDelegatingHandlerV7, errorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot with custom <see cref="DelegatingHandler"/> delegate, but default error mapping is used.
    /// </summary>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QoS behavior by yourself).</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<TProvider>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where TProvider : class, IPollyQoSProvider<HttpResponseMessage>
        => AddPollyV7<TProvider>(builder, delegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by defaults.
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <list type="bullet">
    ///   <item><see cref="GetDelegatingHandlerV7"/></item>
    ///   <item><see cref="DefaultErrorMapping"/></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TProvider">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7<TProvider>(this IOcelotBuilder builder)
        where TProvider : class, IPollyQoSProvider<HttpResponseMessage>
        => AddPollyV7<TProvider>(builder, GetDelegatingHandlerV7, DefaultErrorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by defaults with default QoS provider.
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <list type="bullet">
    ///   <item><see cref="PollyQoSProvider"/></item>
    ///   <item><see cref="GetDelegatingHandlerV7"/></item>
    ///   <item><see cref="DefaultErrorMapping"/></item>
    /// </list>
    /// </remarks>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    [Obsolete("Use AddPolly instead, it will be remove in future version")]
    public static IOcelotBuilder AddPollyV7(this IOcelotBuilder builder)
        => AddPollyV7<PollyQoSProvider>(builder, GetDelegatingHandlerV7, DefaultErrorMapping);

    /// <summary>
    /// Creates default delegating handler based on the <see cref="PollyPoliciesDelegatingHandler"/> type.
    /// </summary>
    /// <param name="route">The downstream route to apply the handler for.</param>
    /// <param name="contextAccessor">The context accessor of the route.</param>
    /// <param name="loggerFactory">The factory of logger.</param>
    /// <returns>A <see cref="DelegatingHandler"/> object, but concrete type is the <see cref="PollyPoliciesDelegatingHandler"/> class.</returns>
    private static DelegatingHandler GetDelegatingHandlerV7(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyPoliciesDelegatingHandler(route, contextAccessor, loggerFactory);

    #endregion
}
