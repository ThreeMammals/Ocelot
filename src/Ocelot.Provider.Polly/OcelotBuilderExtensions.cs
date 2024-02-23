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
    /// <summary>
    /// Default mapping of Polly <see cref="Exception"/>s to <see cref="Error"/> objects.
    /// </summary>
    public static readonly Dictionary<Type, Func<Exception, Error>> DefaultErrorMapping = new Dictionary<Type, Func<Exception, Error>>
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
    /// <typeparam name="T">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QoS behavior by yourself).</param>
    /// <param name="errorMapping">Your customized error mapping.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
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

    /// <summary>
    /// Adds Polly QoS provider to Ocelot with custom error mapping, but default <see cref="DelegatingHandler"/> is used.
    /// </summary>
    /// <typeparam name="T">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="errorMapping">Your customized error mapping.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, Dictionary<Type, Func<Exception, Error>> errorMapping)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPolly<T>(builder, DefaultDelegatingHandler, errorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot with custom <see cref="DelegatingHandler"/> delegate, but default error mapping is used.
    /// </summary>
    /// <typeparam name="T">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <param name="delegatingHandler">Your customized delegating handler (to manage QoS behavior by yourself).</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder, QosDelegatingHandlerDelegate delegatingHandler)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPolly<T>(builder, delegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by defaults.
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <list type="bullet">
    ///   <item><see cref="DefaultDelegatingHandler"/></item>
    ///   <item><see cref="DefaultErrorMapping"/></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">QoS provider to use (by default use <see cref="PollyQoSProvider"/>).</typeparam>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly<T>(this IOcelotBuilder builder)
        where T : class, IPollyQoSProvider<HttpResponseMessage> =>
        AddPolly<T>(builder, DefaultDelegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Adds Polly QoS provider to Ocelot by defaults with default QoS provider.
    /// </summary>
    /// <remarks>
    /// Defaults:
    /// <list type="bullet">
    ///   <item><see cref="PollyQoSProvider"/></item>
    ///   <item><see cref="DefaultDelegatingHandler"/></item>
    ///   <item><see cref="DefaultErrorMapping"/></item>
    /// </list>
    /// </remarks>
    /// <param name="builder">Ocelot builder to extend.</param>
    /// <returns>The reference to the same extended <see cref="IOcelotBuilder"/> object.</returns>
    public static IOcelotBuilder AddPolly(this IOcelotBuilder builder) =>
        AddPolly<PollyQoSProvider>(builder, DefaultDelegatingHandler, DefaultErrorMapping);

    /// <summary>
    /// Creates default delegating handler based on the <see cref="PollyPoliciesDelegatingHandler"/> type.
    /// </summary>
    /// <param name="route">The downstream route to apply the handler for.</param>
    /// <param name="contextAccessor">The context accessor of the route.</param>
    /// <param name="loggerFactory">The factory of logger.</param>
    /// <returns>A <see cref="DelegatingHandler"/> object, but concreate type is the <see cref="PollyPoliciesDelegatingHandler"/> class.</returns>
    public static DelegatingHandler DefaultDelegatingHandler(DownstreamRoute route, IHttpContextAccessor contextAccessor, IOcelotLoggerFactory loggerFactory)
        => new PollyPoliciesDelegatingHandler(route, contextAccessor, loggerFactory);
}
