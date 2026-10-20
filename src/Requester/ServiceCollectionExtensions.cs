using Microsoft.Extensions.DependencyInjection;

namespace Ocelot.Requester;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOcelotMessageInvokerPool(this IServiceCollection services) => services
        .AddSingleton<IHttpRequester, MessageInvokerHttpRequester>()
        .AddSingleton<IMessageInvokerPool, MessageInvokerPool>();
}
