using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ocelot.Requester;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOcelotRequester(this IServiceCollection services)
        => services
        .AddSingleton<IHttpRequester, MessageInvokerHttpRequester>()
        .AddSingleton<IMessageInvokerPool, MessageInvokerPool>();

    public static IServiceCollection AddOcelotRequester<TMessageInvokerPool>(this IServiceCollection services)
        where TMessageInvokerPool : class, IMessageInvokerPool
        => services
        .RemoveAll<IHttpRequester>().AddSingleton<IHttpRequester, MessageInvokerHttpRequester>()
        .RemoveAll<IMessageInvokerPool>().AddSingleton<IMessageInvokerPool, TMessageInvokerPool>();

    public static IServiceCollection AddOcelotRequester<THttpRequester, TMessageInvokerPool>(this IServiceCollection services)
        where THttpRequester : class, IHttpRequester
        where TMessageInvokerPool : class, IMessageInvokerPool
        => services
        .RemoveAll<IHttpRequester>().AddSingleton<IHttpRequester, THttpRequester>()
        .RemoveAll<IMessageInvokerPool>().AddSingleton<IMessageInvokerPool, TMessageInvokerPool>();
}
